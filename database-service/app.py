"""
Traftec VR Simulator – Database Service
Abonnerer på MQTT-topics og lagrer events i PostgreSQL.
"""

import json
import logging
import os
import signal
import sys
import time

import paho.mqtt.client as mqtt
import psycopg2
from psycopg2.extras import Json

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s [%(levelname)s] %(message)s",
    handlers=[logging.StreamHandler(sys.stdout)],
)
log = logging.getLogger("traftec-db")

# ── Konfigurasjon ──────────────────────────────────────────────────────────────

MQTT_HOST = os.getenv("MQTT_HOST", "localhost")
MQTT_PORT = int(os.getenv("MQTT_PORT", "1883"))

TOPICS = [
    ("training/events",  1),
    ("training/session", 1),
    ("training/hse",     1),
]

DB_DSN = (
    f"host={os.getenv('POSTGRES_HOST', 'localhost')} "
    f"port={os.getenv('POSTGRES_PORT', '5432')} "
    f"dbname={os.getenv('POSTGRES_DB', 'traftec_training')} "
    f"user={os.getenv('POSTGRES_USER', 'training')} "
    f"password={os.getenv('POSTGRES_PASSWORD', 'training123')}"
)

_db = None

# ── Database ───────────────────────────────────────────────────────────────────

def get_db():
    global _db
    for attempt in range(1, 11):
        try:
            if _db is None or _db.closed:
                _db = psycopg2.connect(DB_DSN)
                log.info("PostgreSQL tilkoblet")
            return _db
        except psycopg2.OperationalError as e:
            log.warning(f"DB forsøk {attempt}/10: {e}")
            time.sleep(3)
    raise RuntimeError("Kunne ikke koble til PostgreSQL etter 10 forsøk")


def store_event(topic: str, raw: str) -> None:
    global _db
    try:
        data = json.loads(raw)
    except json.JSONDecodeError:
        log.error(f"Ugyldig JSON fra {topic}: {raw!r}")
        return

    # Events now use the nested header / payload / telemetry structure.
    header = data.get("header", {})

    db = get_db()
    try:
        with db.cursor() as cur:
            cur.execute(
                """
                INSERT INTO training_events
                    (event_type, session_id, scenario_id, timestamp, topic, payload)
                VALUES (%s, %s, %s, %s, %s, %s)
                """,
                (
                    header.get("eventType"),
                    header.get("sessionId"),
                    header.get("scenarioId"),
                    header.get("timestamp"),
                    topic,
                    Json(data),
                ),
            )
        db.commit()
        sid = (header.get("sessionId") or "")[:8]
        log.info(f"✓ {header.get('eventType')} | session={sid}... | topic={topic}")
    except Exception as e:
        log.error(f"DB feil: {e}")
        try:
            db.rollback()
        except Exception:
            pass
        _db = None  # Tving re-tilkobling neste gang

# ── MQTT callbacks ─────────────────────────────────────────────────────────────

def on_connect(client, userdata, flags, rc):
    if rc == 0:
        log.info(f"MQTT tilkoblet {MQTT_HOST}:{MQTT_PORT}")
        for topic, qos in TOPICS:
            client.subscribe(topic, qos)
            log.info(f"  Abonnerer: {topic}  (QoS {qos})")
    else:
        log.error(f"MQTT tilkobling avvist, kode={rc}")


def on_disconnect(client, userdata, rc):
    if rc != 0:
        log.warning(f"MQTT uventet frakobling (rc={rc})")


def on_message(client, userdata, msg):
    raw = msg.payload.decode("utf-8", errors="replace")
    log.debug(f"← [{msg.topic}] {raw}")
    store_event(msg.topic, raw)

# ── Hovedløkke ─────────────────────────────────────────────────────────────────

def main():
    get_db()  # Verifiser DB-tilkobling ved oppstart

    client = mqtt.Client(client_id="traftec-db-service", clean_session=True)
    client.on_connect    = on_connect
    client.on_disconnect = on_disconnect
    client.on_message    = on_message

    def shutdown(sig, frame):
        log.info("Avslutter gracefully...")
        client.disconnect()
        if _db and not _db.closed:
            _db.close()
        sys.exit(0)

    signal.signal(signal.SIGINT,  shutdown)
    signal.signal(signal.SIGTERM, shutdown)

    while True:
        try:
            log.info(f"Kobler til MQTT {MQTT_HOST}:{MQTT_PORT}...")
            client.connect(MQTT_HOST, MQTT_PORT, keepalive=60)
            client.loop_forever()
        except Exception as e:
            log.error(f"Feil: {e}. Prøver igjen om 5s...")
            time.sleep(5)


if __name__ == "__main__":
    main()

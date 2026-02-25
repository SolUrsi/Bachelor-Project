# Traftec VR Simulator 🛠️⚡

## Proof of concept 📑
Dette repoet er en samling av kode utviklet som et "proof-of-concept" for Traftec's VR Simulator som en del av IKT302-G 26V Bacheloroppgave for Dataingeniør på UiA Grimstad. Hensikten med prosjektet er å kunne bevise ovenfor Traftec at VR kan bidra med opplæring av deres lærlinger og montører på interne HMS rutiner og samtidig la dem "arbeide" under spenning og arbeidsforhold uten ellers eksponering for fare eller støt. Om dette prosjektet faller i smak vil Traftec videreutvikle konseptet for eget bruk. 

## Rammeverk 🖥️
Prosjektet er utviklet i [Unity](https://unity.com/) for Meta plattformens VR briller, da spesifikt [Meta Quest 3](https://www.meta.com/no/quest/quest-3/).

## Strategi 📊
Prosjektet totalt utvikles av fire utviklere, hvorav tre av dem er Dataingeniør studenter som er tilsatt sine egne "origin branches" og en av dem en AI student som jobber med en virtuell AI assistent for simulatoren. Hver tilsatte gren bygger opp mot en `dev` gren som brukes for å løse konflikter og klargjøre sammenfelte oppdateringer for å dyttes ut på `main`. Tankegangen er da;

1. Utvikler har utviklet og testet sin kode lokalt på VR briller.
2. Utvikler "pusher" sin kode opp på sin "origin branch".
3. Utvikler "merger" sin "origin branch" med `dev` og bruker auto konflikt løsning for å løse eventuelle konflikter i koden.
4. Repo administrator, ved Scrum møte, "merger" `dev` med `main` slik at utviklere kan kjøre gjennom total simulasjonen og teste i felles.
5. `main` versjoneres og er klar for opplastning som en `.apk` på den aktuelle VR brillen.

## Beskrivelse av Simulator

--- Masse beskrivelse, steg-for-steg over hvordan simulatoren spilles gjennom ---

## Mål 🏁
Når VR simulatoren er ferdig utviklet skal den brukes for å måle læringsoppnåelse hos lærlinger hvor igjen den aktuelle statistikken skal fremvises slik at Traftec kan ta en beslutning på om prosjektet er verd å investere mer tid og ressurser i. Disse resultatene vil bli tilgjengeligjort som en del av bachelor oppgaven. 

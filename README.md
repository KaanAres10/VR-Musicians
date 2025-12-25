# VR Musicians

A **two player asymmetric VR experience** where **music directly controls gameplay**.

![Cover](Docs/cover.jpg)

In this project, **one player selects a Spotify song**, while **the other plays in VR**. The VR world dynamically reacts to the **genre and energy** of the selected music in real time changing environments, post processing effects, and gameplay. This creates a unique interactive experience where **music becomes a gameplay mechanic**, and one player directly influences the other through song choice.

---

## Video
[![Watch the demo]](https://github.com/user-attachments/assets/cb1f20aa-1923-421c-ae5b-ad5e28e418a6)


## Core Concept

### Player 1: VR Player
- Experiences a VR world and gameplay for selected track

### Player 2: Music Controller
- Selects songs from Spotify playlists  
- Can choose **genre-specific playlists** or a **random playlist**

As the song changes:
- Environment themes switch
- Post-processing effects update
- Enemy spawn rates and difficulty adjust
- Weapons and gameplay mechanics change

---

## Spotify Integration

<img src="Docs/Spotify.png" width="300"/>

- We use plugin to connect Unity
- Fetches real-time data from the **Spotify Web API and ReccoBeats**

Extracted data:
- Track genre
- Audio features (energy, etc.)

These values drive **scene selection, visuals, and gameplay logic** in real time.

---

## Genre-Based Environments & Gameplay

Each genre has a **unique environment, weapon, and gameplay mechanic**:

### Classical
- **Weapon:** Violin 
- **Gameplay:** Defensive, controlled pacing
- **Mechanics:**
  - Increased health regeneration
  - Fewer enemies spawn

<img src="Docs/classic.png" width="350"/>

---

### Pop
- **Weapon:** Disco ball
  - Roll it like a bowling ball
  - Release to launch at enemies
- **Gameplay:** Physics Based combat
- - **Mechanics:**
  - Increased enemy spawns
  - No health regeneration
  - Score-focused playstyle

<img src="Docs/pop1.png" width="350"/>

---

### Country
- **Weapon:** Revolver
- **Gameplay:** Tactical shooting
- **Mechanics:**
  - Increased health regeneration
  - Moderate enemy spawns

<img src="Docs/country.png" width="350"/>

---

### Rock
- **Weapon:** Machine gun
- **Gameplay:** High intensity
- **Mechanics:**
  - Increased enemy spawns
  - No health regeneration
  - Score focused playstyle

<img src="Docs/rock.png" width="350"/>

---

### Rap
- **Weapon:** Baseball bat 
- **Gameplay:** Aggressive close combat
- **Mechanics:**
  - Increased enemy spawns
  - No health regeneration
  - Score focused playstyle

<img src="Docs/rap.png" width="350"/>

---

## Energy & Difficulty System

### Low-energy genres (Classical, Country)
- Health regeneration enabled
- Fewer enemies
- Slower pacing

### High-energy genres (Rock, Pop, Rap)
- Increased enemy spawns
- No health regeneration
- Higher scoring potential

This ensures each genre feels **mechanically different**.

---

## Playlists & Score Multipliers

- Genre specific playlists on Spotift for different experiences
- **Special “Random” playlist**:
  - Plays unpredictable songs
  - Double score multiplier

This introduces **risk reward gameplay**.

---

## Enemy Models

<img src="Docs/character.jpg" width="400"/>

---

## Tech Stack

- **Engine:** Unity 6  
- **VR:** Meta Quest  
- **Music Data:** Spotify Web API, ReccoBeats  
- **Platform:** PCVR

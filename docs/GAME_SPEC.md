# Crosswire Game Specification

## Status

This document records only decisions locked for the MVP. Anything identified as unresolved is not yet part of the design contract.

## Game identity

- Title: **Crosswire**
- Genre: top-down 2D multiplayer party game
- MVP player count: 3–6 players
- MVP content: one arena
- Each player owns one toy/bumper car.

## Core control rule

Each player controls another player's car rather than their own.

Control assignments form one randomized cycle containing every player. As a result:

- nobody controls their own car;
- every car has exactly one controller;
- every player controls exactly one car; and
- no mutual two-player control pair is possible.

For example, a valid four-player cycle is `A → B → C → D → A`. Separate cycles or pairs are not valid assignments.

## Round information

- During the first half, the identity of the player driving each car is hidden.
- At halftime, driver identities are revealed.
- The control mapping currently remains unchanged during the second half.
- There is no elimination.

The exact duration of each half and the full round are unresolved.

## Controls

- Cars auto-accelerate.
- Players steer left or right.
- Players have one boost action.

## Unresolved decisions

The following are explicitly **UNRESOLVED**:

- final scoring model;
- final objective/economy;
- revenge mechanic;
- exact round timing; and
- premium content details.

No implementation should assume an answer to an unresolved decision without first updating this specification.


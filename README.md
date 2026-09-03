# VR_TP1

VR minigolf prototype in a prison (Cardboard / iPhone).

## How to play

1. Open `GameScene` in Unity and press Play (or build to iOS).
2. The player **does not move** — only turn your head to aim.
3. **Hold** a VR BOX joystick button (or touch) to charge power.
4. **Release** to hit the ball.
5. Goal: **hole in one** into the vase at the end of the corridor.
6. If you miss, press again to retry.

Gameplay wires itself when the scene loads (`MinigolfBootstrap`).
HUD is a world-space canvas on the camera (visible in Cardboard).

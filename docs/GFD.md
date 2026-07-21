# Game Feel Document

## Feedback Matrix

| Event | Emotion | Visual | Audio | Kinesthetic | Camera | Haptics | Hitstop / Time Scale | Timing | Assets |
|---|---|---|---|---|---|---|---|---|---|
| Tutorial collector tap prompt | Clear, inviting, easy to follow | Translucent white circle expands and fades behind the fingertip at the tracked collector | Existing tap SFX plays when the player taps | Finger moves down and compresses in sync with the circle | None | None | None; use unscaled time | 1.5 s looping cycle; visible only during `TapWaiting` and `TapQueue` | Procedural uGUI circle; no external asset |
| Collector successful shot | Playful, responsive, lightly forceful | Existing pooled white projectile and block hit punch | Existing Consume SFX | Pig visual recoils opposite the shot direction with a damped three-oscillation shake; gameplay root stays stable | None | None | None | 0.12 s unscaled-time shake; 0.045 world-unit peak; repeated shots restart rather than stack | Existing local systems only; no external asset |

## Tuning Notes

- The tap pulse and finger press share one normalized phase so their attack and release stay synchronized.
- The circle is created once during UI initialization and reused; no per-frame allocation or particle spawning.
- Rule of Three is satisfied by the pulse visual, existing tap audio, and finger compression/movement.
- Collector shot feedback shares one trigger across the projectile, Consume SFX, and pig recoil.
- Shot recoil animates a dedicated visual pivot so Rigidbody2D movement, collision, and the capacity label remain stable.
- Repeated shots restart the 0.12-second envelope; disable and pool reset restore the authored visual pose.

# Pixel Flow Clone — Feature Scenarios

The original design overview remains in `[Pixel Flow Clone]-GDD.md`. This canonical workflow file records testable Gherkin scenarios for implemented features.

## Gameplay Instruction Tap Pulse

```gherkin
Scenario: Tutorial finger highlights the collector that must be tapped
  Given level 1 is waiting for a tap on the tutorial collector
  When the Finger Instruction is visible
  Then a translucent white circle is shown behind the fingertip at the target collector
  And the circle expands and fades over a 1.5 second unscaled-time cycle
  And the finger press motion follows the same cycle

Scenario: Tutorial tap pulse is hidden outside actionable steps
  Given the tutorial is waiting for a collector lap or has completed
  When the Finger Instruction updates
  Then the finger and tap pulse are hidden
```

## Collector Shot Shake

```gherkin
Scenario: Collector gives light recoil after a successful shot
  Given a CollectorUnit is moving on the conveyor with remaining capacity
  When it successfully fires at and consumes a matching block
  Then its pig visual recoils opposite the shot direction
  And the visual performs a damped three-oscillation shake over 0.12 seconds
  And its Rigidbody2D, collider, conveyor position, and capacity label are not displaced

Scenario: Collector shot shake is interrupted safely
  Given a CollectorUnit is currently playing its shot shake
  When it fires again, is disabled, or returns to the pool
  Then repeated fire restarts the shake without stacking offsets
  And lifecycle interruption immediately restores the pig visual to its authored local position
```

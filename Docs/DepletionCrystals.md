# Depletion crystal regression

Symptom: fully harvested resource hexes stopped leaving ground experience crystals.
Cause: ResourceRequester.Start bound every child TimerController to the production owner. Forest 1 has a 4.8-second production timer and a separate 0.5-second cleanup timer. Both were bound. After production consumed the worker, cleanup could not start; PortalTowerProgression.WaitForClearedHex kept waiting for object deactivation.
Fix: bind only timers whose persistent completion callback invokes FinishProcessing on this requester. Worker evacuation pauses only owned production timers.
Validation: isolated Play Mode fixture using Forest 1, Stone 1, Chickens 1 and Boars 1. Executed real final production callbacks and cleanup timers. All four objects deactivated; the deferred drop produced 1 to 5 ground experience crystals per cleared hex. No saves or resource inventory modified.
Existing rule: crystals remain disabled during the first tutorial expedition before crystal technology is available.

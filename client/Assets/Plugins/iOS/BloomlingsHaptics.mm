// The game's haptics on iOS (spec 001 FR-073, spec 005 FR-042): impact feedback of a style and an intensity, played now
// or after a short delay, for the C# side's Haptics.Play (client/Assets/Bloomlings/Services/Feedback/GameFeedback.cs).
// Styles: 0 light (a tick), 1 soft (a low tick), 2 rigid (a click), 3 medium, 4 heavy. One generator per style, kept
// prepared, so a tick lands with its sound. iOS 15+ (the plan's target), so every style and intensity is there.

#import <UIKit/UIKit.h>

static UIImpactFeedbackGenerator *BloomlingsGenerators[5];

static UIImpactFeedbackGenerator *BloomlingsGenerator(int style)
{
    if (BloomlingsGenerators[style] == nil)
    {
        UIImpactFeedbackStyle styles[5] = {
            UIImpactFeedbackStyleLight,
            UIImpactFeedbackStyleSoft,
            UIImpactFeedbackStyleRigid,
            UIImpactFeedbackStyleMedium,
            UIImpactFeedbackStyleHeavy,
        };
        BloomlingsGenerators[style] = [[UIImpactFeedbackGenerator alloc] initWithStyle:styles[style]];
        [BloomlingsGenerators[style] prepare];
    }

    return BloomlingsGenerators[style];
}

static void BloomlingsImpactNow(int style, float intensity)
{
    UIImpactFeedbackGenerator *generator = BloomlingsGenerator(style);
    [generator impactOccurredWithIntensity:MAX(0.0f, MIN(1.0f, intensity))];
    [generator prepare];
}

extern "C" void BloomlingsHapticImpact(int style, float intensity, int delayMilliseconds)
{
    if (style < 0 || style > 4)
    {
        return;
    }

    if (delayMilliseconds <= 0 && [NSThread isMainThread])
    {
        BloomlingsImpactNow(style, intensity);
        return;
    }

    dispatch_after(dispatch_time(DISPATCH_TIME_NOW, (int64_t)MAX(0, delayMilliseconds) * NSEC_PER_MSEC), dispatch_get_main_queue(), ^{
        BloomlingsImpactNow(style, intensity);
    });
}

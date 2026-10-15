namespace Bloomlings.Client.UI.Design
{
    public static class SpecialObjectLook
    {
        public const float CounterShare = 0.42f;
        public const float CounterDigitScale = 1.5f;
        public static float CounterFontShare => CounterShare * CounterDigitScale * DesignTokens.Type.Badge.Size / 48f;
    }
}

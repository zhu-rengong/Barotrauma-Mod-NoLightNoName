namespace NoLightNoName;

public static class ColorExtensions
{
    public static float GetRelativeLuminance(this Color color)
    {
        float Linearize(float channel)
        {
            float c = channel / 255f;

            if (c <= 0.04045f)
                return c / 12.92f;
            else
                return MathF.Pow((c + 0.055f) / 1.055f, 2.4f);
        }

        float r = Linearize(color.R);
        float g = Linearize(color.G);
        float b = Linearize(color.B);

        return 0.2126f * r + 0.7152f * g + 0.0722f * b;
    }

    public static byte GetLuminanceByte(this Color color)
    {
        return (byte)(GetRelativeLuminance(color) * 255f);
    }
}
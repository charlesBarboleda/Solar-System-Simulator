using Unity.Mathematics;

public readonly struct DataValue
{
    public readonly string RawTextValue;
    public readonly double NumericValue;
    public readonly string StringValue;

    public DataValue(string rawTextValue, double numericValue)
    {
        RawTextValue = rawTextValue;
        NumericValue = numericValue;
        StringValue = numericValue != 0.0 ? string.Empty : rawTextValue;
    }
}
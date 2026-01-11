using System;

public readonly struct ParsableDataValue
{
    public string RawTextValue { get; }
    public double NumericValue { get; }
    public UnitMeasurements NumericValueUnit { get; }
    public string StringValue { get; }
    public bool IsNumeric { get; }

    public ParsableDataValue(string rawTextValue, double numericValue, UnitMeasurements unitMeasurements = UnitMeasurements.None)
    {
        if (string.IsNullOrWhiteSpace(rawTextValue)) throw new ArgumentException("[ParsableDateValue] ParsableDateValue(): rawTextValue cannot be null/empty.", nameof(rawTextValue));

        RawTextValue = rawTextValue;
        NumericValue = numericValue;
        NumericValueUnit = unitMeasurements;
        StringValue = null;
        IsNumeric = true;
    }

    public ParsableDataValue(string rawTextValue, string stringValue)
    {
        if (string.IsNullOrWhiteSpace(rawTextValue))
            throw new ArgumentException("[ParsableDateValue] ParsableDateValue(): rawTextValue cannot be null/empty.", nameof(rawTextValue));

        RawTextValue = rawTextValue;
        StringValue = stringValue ?? throw new ArgumentNullException(nameof(stringValue));
        NumericValue = default;
        NumericValueUnit = UnitMeasurements.None;
        IsNumeric = false;
    }
}

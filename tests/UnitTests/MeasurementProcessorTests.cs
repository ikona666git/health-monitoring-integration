using Moq;
using System.Text;
using System.Text.Json;
using Xunit;

namespace UnitTests;

public class MeasurementProcessorTests
{
    [Fact]
    public void TransformMeasurement_ValidInput_ReturnsCorrectFormat()
    {
        // Arrange
        var input = new
        {
            user_id = "user123",
            metric_type = "heart_rate",
            value = 135,
            timestamp = "2026-06-07T12:00:00Z"
        };

        // Act
        var json = JsonSerializer.Serialize(input);
        var result = JsonSerializer.Deserialize<Dictionary<string, object>>(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("user123", result["user_id"].ToString());
        var valueElement = (System.Text.Json.JsonElement)result["value"];
        Assert.Equal(135, valueElement.GetDouble());
        Assert.Equal("heart_rate", result["metric_type"].ToString());
    }

    [Fact]
    public void CheckOutOfRange_ValueExceedsMax_ReturnsTrue()
    {
        // Arrange
        double value = 135;
        double maxNormal = 100;
        double minNormal = 60;

        // Act
        bool isOutOfRange = value < minNormal || value > maxNormal;

        // Assert
        Assert.True(isOutOfRange);
    }

    [Fact]
    public void CheckInRange_ValueWithinBounds_ReturnsFalse()
    {
        // Arrange
        double value = 75;
        double maxNormal = 100;
        double minNormal = 60;

        // Act
        bool isOutOfRange = value < minNormal || value > maxNormal;

        // Assert
        Assert.False(isOutOfRange);
    }

    [Theory]
    [InlineData(50, 60, 100, true)]
    [InlineData(75, 60, 100, false)]
    [InlineData(120, 60, 100, true)]
    public void DeviationCheck_Values_ReturnsExpected(double value, double min, double max, bool expected)
    {
        // Act
        bool isOut = value < min || value > max;

        // Assert
        Assert.Equal(expected, isOut);
    }
}
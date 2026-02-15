using Moq;

namespace StoreApi.Tests;

// Example interface to mock
public interface ICalculator
{
    int Add(int a, int b);
    int Multiply(int a, int b);
}

// Example service that depends on ICalculator
public class MathService
{
    private readonly ICalculator _calculator;

    public MathService(ICalculator calculator)
    {
        _calculator = calculator;
    }

    public int CalculateSum(int a, int b)
    {
        return _calculator.Add(a, b);
    }

    public int CalculateProduct(int a, int b)
    {
        return _calculator.Multiply(a, b);
    }
}

// Simple class without dependencies - no need to mock
public class StringHelper
{
    public string Reverse(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        char[] chars = input.ToCharArray();
        Array.Reverse(chars);
        return new string(chars);
    }

    public bool IsPalindrome(string input)
    {
        if (string.IsNullOrEmpty(input))
            return false;

        string reversed = Reverse(input);
        return input.Equals(reversed, StringComparison.OrdinalIgnoreCase);
    }

    public int CountVowels(string input)
    {
        if (string.IsNullOrEmpty(input))
            return 0;

        return input.Count(c => "aeiouAEIOU".Contains(c));
    }
}

/// <summary>
/// Example tests demonstrating XUnit best practices with and without mocking.
/// </summary>
[Trait("Category", "Unit")]
public class MathServiceTests
{
    [Fact]
    public void CalculateSum_ShouldReturnCorrectSum()
    {
        // Arrange - Setup mock
        var mockCalculator = new Mock<ICalculator>();
        mockCalculator.Setup(x => x.Add(2, 3)).Returns(5);

        var service = new MathService(mockCalculator.Object);

        // Act
        var result = service.CalculateSum(2, 3);

        // Assert
        Assert.Equal(5, result);
        mockCalculator.Verify(x => x.Add(2, 3), Times.Once);
    }

    [Fact]
    public void CalculateProduct_ShouldReturnCorrectProduct()
    {
        // Arrange
        var mockCalculator = new Mock<ICalculator>();
        mockCalculator.Setup(x => x.Multiply(4, 5)).Returns(20);

        var service = new MathService(mockCalculator.Object);

        // Act
        var result = service.CalculateProduct(4, 5);

        // Assert
        Assert.Equal(20, result);
        mockCalculator.Verify(x => x.Multiply(4, 5), Times.Once);
    }

    [Fact]
    public void Calculator_ShouldVerifyMethodWasNeverCalled()
    {
        // Arrange
        var mockCalculator = new Mock<ICalculator>();
        var service = new MathService(mockCalculator.Object);

        // Act - Don't call anything

        // Assert
        mockCalculator.Verify(x => x.Add(It.IsAny<int>(), It.IsAny<int>()), Times.Never);
    }
}

/// <summary>
/// Tests for StringHelper demonstrating data-driven testing with [Theory] and [InlineData].
/// </summary>
[Trait("Category", "Unit")]
public class StringHelperTests
{
    private readonly StringHelper _helper = new();

    [Fact]
    public void Reverse_ShouldReverseString()
    {
        // Arrange & Act
        var result = _helper.Reverse("hello");

        // Assert
        Assert.Equal("olleh", result);
    }

    [Theory]
    [InlineData("racecar", true)]
    [InlineData("Madam", true)]
    [InlineData("A", true)]
    [InlineData("hello", false)]
    [InlineData("world", false)]
    public void IsPalindrome_WithVariousInputs_ReturnsExpectedResult(string input, bool expected)
    {
        // Act
        var result = _helper.IsPalindrome(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsPalindrome_WithEmptyOrNull_ReturnsFalse(string? input, bool expected)
    {
        // Act
        var result = _helper.IsPalindrome(input!);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("", 0)]
    [InlineData("bcdfg", 0)]
    [InlineData("aeiou", 5)]
    [InlineData("AEIOU", 5)]
    [InlineData("Programming", 3)]
    [InlineData("hello world", 3)]
    public void CountVowels_VariousInputs_ShouldReturnCorrectCount(string input, int expected)
    {
        // Act
        var result = _helper.CountVowels(input);

        // Assert
        Assert.Equal(expected, result);
    }
}

/// <summary>
/// Demonstrates [MemberData] for complex test data that can't fit in InlineData.
/// </summary>
[Trait("Category", "Unit")]
public class MemberDataExampleTests
{
    public static IEnumerable<object[]> ReverseTestData =>
        new List<object[]>
        {
            new object[] { "hello", "olleh" },
            new object[] { "world", "dlrow" },
            new object[] { "12345", "54321" },
            new object[] { "", "" },
            new object[] { "a", "a" }
        };

    [Theory]
    [MemberData(nameof(ReverseTestData))]
    public void Reverse_WithMemberData_ReturnsExpectedResult(string input, string expected)
    {
        // Arrange
        var helper = new StringHelper();

        // Act
        var result = helper.Reverse(input);

        // Assert
        Assert.Equal(expected, result);
    }
}
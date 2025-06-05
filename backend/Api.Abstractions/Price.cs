namespace Api.Abstractions;

public sealed record Price(
    decimal Value,
    string Currency);
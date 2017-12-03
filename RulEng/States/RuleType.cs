namespace RulEng.States
{
    public enum RuleType
    {
        Error = -1,
        Unknown = 0,
        Exists,
        HasMeaningfulValue,
        LessThan,
        Equal,
        GreaterThan,
        RegularExpression,
        And,
        Or,
        Xor
    }
}

using System.ComponentModel;

namespace Spamma.App.Client.Infrastructure.Helpers;

public static class Extensions
{
    public static string GetDescription<T>(this T enumerationValue)
        where T : struct
    {
        var type = enumerationValue.GetType();
        if (!type.IsEnum)
        {
            throw new ArgumentException("EnumerationValue must be of Enum type", "enumerationValue");
        }

        if (enumerationValue.ToString() == null)
        {
            return string.Empty;
        }

        var memberInfo = type.GetMember(enumerationValue.ToString()!);
        if (memberInfo.Length > 0)
        {
            object[] attrs = memberInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

            if (attrs.Length > 0)
            {
                return ((DescriptionAttribute)attrs[0]).Description;
            }
        }

        return enumerationValue.ToString()!;
    }
}
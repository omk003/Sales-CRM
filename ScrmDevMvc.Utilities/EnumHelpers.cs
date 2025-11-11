using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;

namespace scrm_dev_mvc.Utilities
{
    public static class EnumHelpers
    {
        // This is an "extension method" for all Enums
        public static string GetDisplayName(this Enum enumValue)
        {
            return enumValue.GetType()
                .GetMember(enumValue.ToString())
                .First()
                .GetCustomAttribute<DisplayAttribute>()?
                .GetName() ?? enumValue.ToString(); // Fallback to the default name
        }
    }
}
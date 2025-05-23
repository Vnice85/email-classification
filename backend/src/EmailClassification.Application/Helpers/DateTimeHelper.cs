namespace EmailClassification.Application.Helpers
{
    public static class DateTimeHelper
    {

        public static string? FormatToVietnamTime(DateTime? dateTime)
        {
            if (dateTime == null)
                return null;
            var vietnamTime = dateTime?.ToUniversalTime().AddHours(7);
            return vietnamTime?.ToString("yyyy/MM/dd HH:mm:ss");
        }

        public static DateTime? DateTimeFromString(string dateTimeString)
        {
            if (DateTime.TryParse(dateTimeString, out DateTime tmp))
            {
                tmp = NormalizeDateTime(tmp);
                return tmp;
            }
            return null;
        }

        public static DateTime NormalizeDateTime(DateTime dateTime)
        {
            if (dateTime.Kind == DateTimeKind.Utc)
                return dateTime;

            if (dateTime.Kind == DateTimeKind.Local)
                return dateTime.ToUniversalTime();

            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }
    }
}

namespace Jobs;

//
// Summary:
//     Helper class that provides common values for the cron expressions.
public class Cron(string value)
{
    private readonly string _value = value;
    public string Value => _value;

    //
    // Summary:
    //     Returns cron expression that fires every minute.
    public static Cron Minutely()
    {
        return new Cron("* * * * *");
    }

    //
    // Summary:
    //     Returns cron expression that fires every hour at the first minute.
    public static Cron Hourly()
        => Hourly(0);

    //
    // Summary:
    //     Returns cron expression that fires every hour at the specified minute.
    //
    // Parameters:
    //   minute:
    //     The minute in which the schedule will be activated (0-59).
    public static Cron Hourly(int minute)
    {
        return new Cron($"{minute} * * * *");
    }

    //
    // Summary:
    //     Returns cron expression that fires every day at 00:00 UTC.
    public static Cron Daily()
    {
        return Daily(0);
    }

    //
    // Summary:
    //     Returns cron expression that fires every day at the first minute of the specified
    //     hour in UTC.
    //
    // Parameters:
    //   hour:
    //     The hour in which the schedule will be activated (0-23).
    public static Cron Daily(int hour)
    {
        return Daily(hour, 0);
    }

    //
    // Summary:
    //     Returns cron expression that fires every day at the specified hour and minute
    //     in UTC.
    //
    // Parameters:
    //   hour:
    //     The hour in which the schedule will be activated (0-23).
    //
    //   minute:
    //     The minute in which the schedule will be activated (0-59).
    public static Cron Daily(int hour, int minute)
    {
        return new Cron($"{minute} {hour} * * *");
    }

    //
    // Summary:
    //     Returns cron expression that fires every week at Monday, 00:00 UTC.
    public static Cron Weekly()
    {
        return Weekly(DayOfWeek.Monday);
    }

    //
    // Summary:
    //     Returns cron expression that fires every week at 00:00 UTC of the specified day
    //     of the week.
    //
    // Parameters:
    //   dayOfWeek:
    //     The day of week in which the schedule will be activated.
    public static Cron Weekly(DayOfWeek dayOfWeek)
    {
        return Weekly(dayOfWeek, 0);
    }

    //
    // Summary:
    //     Returns cron expression that fires every week at the first minute of the specified
    //     day of week and hour in UTC.
    //
    // Parameters:
    //   dayOfWeek:
    //     The day of week in which the schedule will be activated.
    //
    //   hour:
    //     The hour in which the schedule will be activated (0-23).
    public static Cron Weekly(DayOfWeek dayOfWeek, int hour)
    {
        return Weekly(dayOfWeek, hour, 0);
    }

    //
    // Summary:
    //     Returns cron expression that fires every week at the specified day of week, hour
    //     and minute in UTC.
    //
    // Parameters:
    //   dayOfWeek:
    //     The day of week in which the schedule will be activated.
    //
    //   hour:
    //     The hour in which the schedule will be activated (0-23).
    //
    //   minute:
    //     The minute in which the schedule will be activated (0-59).
    public static Cron Weekly(DayOfWeek dayOfWeek, int hour, int minute)
    {
        return new Cron($"{minute} {hour} * * {(int)dayOfWeek}");
    }

    //
    // Summary:
    //     Returns cron expression that fires every month at 00:00 UTC of the first day
    //     of month.
    public static Cron Monthly()
    {
        return Monthly(1);
    }

    //
    // Summary:
    //     Returns cron expression that fires every month at 00:00 UTC of the specified
    //     day of month.
    //
    // Parameters:
    //   day:
    //     The day of month in which the schedule will be activated (1-31).
    public static Cron Monthly(int day)
    {
        return Monthly(day, 0);
    }

    //
    // Summary:
    //     Returns cron expression that fires every month at the first minute of the specified
    //     day of month and hour in UTC.
    //
    // Parameters:
    //   day:
    //     The day of month in which the schedule will be activated (1-31).
    //
    //   hour:
    //     The hour in which the schedule will be activated (0-23).
    public static Cron Monthly(int day, int hour)
    {
        return Monthly(day, hour, 0);
    }

    //
    // Summary:
    //     Returns cron expression that fires every month at the specified day of month,
    //     hour and minute in UTC.
    //
    // Parameters:
    //   day:
    //     The day of month in which the schedule will be activated (1-31).
    //
    //   hour:
    //     The hour in which the schedule will be activated (0-23).
    //
    //   minute:
    //     The minute in which the schedule will be activated (0-59).
    public static Cron Monthly(int day, int hour, int minute)
    {
        return new Cron($"{minute} {hour} {day} * *");
    }

    //
    // Summary:
    //     Returns cron expression that fires every year on Jan, 1st at 00:00 UTC.
    public static Cron Yearly()
    {
        return Yearly(1);
    }

    //
    // Summary:
    //     Returns cron expression that fires every year in the first day at 00:00 UTC of
    //     the specified month.
    //
    // Parameters:
    //   month:
    //     The month in which the schedule will be activated (1-12).
    public static Cron Yearly(int month)
    {
        return Yearly(month, 1);
    }

    //
    // Summary:
    //     Returns cron expression that fires every year at 00:00 UTC of the specified month
    //     and day of month.
    //
    // Parameters:
    //   month:
    //     The month in which the schedule will be activated (1-12).
    //
    //   day:
    //     The day of month in which the schedule will be activated (1-31).
    public static Cron Yearly(int month, int day)
    {
        return Yearly(month, day, 0);
    }

    //
    // Summary:
    //     Returns cron expression that fires every year at the first minute of the specified
    //     month, day and hour in UTC.
    //
    // Parameters:
    //   month:
    //     The month in which the schedule will be activated (1-12).
    //
    //   day:
    //     The day of month in which the schedule will be activated (1-31).
    //
    //   hour:
    //     The hour in which the schedule will be activated (0-23).
    public static Cron Yearly(int month, int day, int hour)
    {
        return Yearly(month, day, hour, 0);
    }

    //
    // Summary:
    //     Returns cron expression that fires every year at the specified month, day, hour
    //     and minute in UTC.
    //
    // Parameters:
    //   month:
    //     The month in which the schedule will be activated (1-12).
    //
    //   day:
    //     The day of month in which the schedule will be activated (1-31).
    //
    //   hour:
    //     The hour in which the schedule will be activated (0-23).
    //
    //   minute:
    //     The minute in which the schedule will be activated (0-59).
    public static Cron Yearly(int month, int day, int hour, int minute)
    {
        return new Cron($"{minute} {hour} {day} {month} *");
    }
}
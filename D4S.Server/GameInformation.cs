namespace D4S.Host
{
    using System;
    using System.Collections.Generic;

    public class GameInformation
    {
        public string Name { get; private set; }
        public Dictionary<DayOfWeek, DateTime> Schedule { get; private set; }

        public GameInformation(string name, string schedule)
        {
            this.Name = name;
            this.Schedule = new Dictionary<DayOfWeek, DateTime>();

            this.ParseSchedule(schedule);
        }

        private void ParseSchedule(string schedule)
        {
            string[] times = schedule.Split(new char[] { '#' }, StringSplitOptions.RemoveEmptyEntries);

            if (times.Length == 1)
            {
                try
                {
                    DateTime time = new DateTime(1, 1, 1, int.Parse(times[0]), 0, 0);

                    this.Schedule.Add(DayOfWeek.Monday, time);
                    this.Schedule.Add(DayOfWeek.Tuesday, time);
                    this.Schedule.Add(DayOfWeek.Wednesday, time);
                    this.Schedule.Add(DayOfWeek.Thursday, time);
                    this.Schedule.Add(DayOfWeek.Friday, time);
                    this.Schedule.Add(DayOfWeek.Saturday, time);
                    this.Schedule.Add(DayOfWeek.Sunday, time);
                }
                catch (Exception)
                {
                    throw new ApplicationException($"Invalid schedule {schedule} for game {this.Name}");
                }
            }
            else if (times.Length == 7)
            {
                try
                {
                    this.Schedule.Add(DayOfWeek.Monday, new DateTime(1, 1, 1, int.Parse(times[0]), 0, 0));
                    this.Schedule.Add(DayOfWeek.Tuesday, new DateTime(1, 1, 1, int.Parse(times[1]), 0, 0));
                    this.Schedule.Add(DayOfWeek.Wednesday, new DateTime(1, 1, 1, int.Parse(times[2]), 0, 0));
                    this.Schedule.Add(DayOfWeek.Thursday, new DateTime(1, 1, 1, int.Parse(times[3]), 0, 0));
                    this.Schedule.Add(DayOfWeek.Friday, new DateTime(1, 1, 1, int.Parse(times[4]), 0, 0));
                    this.Schedule.Add(DayOfWeek.Saturday, new DateTime(1, 1, 1, int.Parse(times[5]), 0, 0));
                    this.Schedule.Add(DayOfWeek.Sunday, new DateTime(1, 1, 1, int.Parse(times[6]), 0, 0));
                }
                catch (Exception)
                {
                    throw new ApplicationException($"Invalid schedule {schedule} for game {this.Name}");
                }
            }
            else
            {
                throw new ApplicationException($"Invalid schedule {schedule} for game {this.Name}");
            }
        }
    }
}
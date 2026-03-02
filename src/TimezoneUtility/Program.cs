using System.CommandLine;
using TimezoneUtility.Commands;

// Root command
var rootCommand = new RootCommand("Timezone utility for managing times across different timezones")
{
    Name = "tzutil"
};

// Add commands
rootCommand.AddCommand(NowCommand.Create());
rootCommand.AddCommand(ConvertCommand.Create());
rootCommand.AddCommand(MeetingCommand.Create());
rootCommand.AddCommand(DashboardCommand.Create());
rootCommand.AddCommand(ProfileCommand.Create());
rootCommand.AddCommand(ConfigCommand.Create());

return await rootCommand.InvokeAsync(args);

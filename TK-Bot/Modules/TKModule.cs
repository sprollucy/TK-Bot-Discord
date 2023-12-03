using Discord.Commands;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace TK_Bot.Modules
{
    public class TKModule : ModuleBase<SocketCommandContext>
    {
        private const string JsonFileName = "TeamKillData.json";

        private Dictionary<string, List<string>> teamKillList;
        private Dictionary<string, int> playerCounts;

        public TKModule()
        {
            LoadData();
        }

        private void LoadData()
        {
            if (File.Exists(JsonFileName))
            {
                var json = File.ReadAllText(JsonFileName);
                var data = JsonConvert.DeserializeObject<TKModuleData>(json);

                teamKillList = data.TeamKillList;
                playerCounts = data.PlayerCounts;
            }
            else
            {
                teamKillList = new Dictionary<string, List<string>>();
                playerCounts = new Dictionary<string, int>();
            }
        }

        private void SaveData()
        {
            var data = new TKModuleData
            {
                TeamKillList = teamKillList,
                PlayerCounts = playerCounts
            };

            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(JsonFileName, json);
        }

        [Command("help")]
        public async Task HelpAsync()
        {
            var helpMessage = "!tkadd {playername} {killed} {map}\n" +
                              "!tklist\n" +
                              "!tkremove {number}\n" +
                              "!scoreboard\n" +
                              "!about\n" + 
                              "For help on commands do '!help {commandname}' I.E '!help tkadd'";

            await ReplyAsync(helpMessage);
        }

        [Command("about")]
        public async Task AboutAsync()
        {
            await ReplyAsync("Created by Sprollucy. Find more of my work at https://github.com/sprollucy");

        }

        [Command("help tkadd")]
        public async Task HelpTkadd()
        {
            await ReplyAsync("!tkadd {playername} {killed} {map}. Example !tkadd Sprollucy Fratzog Woods. Date and time are automatically assigned and can be adjusted in the log file");
        }

        [Command("help tklist")]
        public async Task HelpTkList()
        {
            await ReplyAsync("Prints all the log entries for added team kills");
        }

        [Command("help tkremove")]
        public async Task HelpTkremove()
        {
            await ReplyAsync("!tkremove {number} Removes a entry from the tk log list(tk number is from !tklist). I.E !tkremove 4");
        }

        [Command("help scoreboard")]
        public async Task Helpscoreboard()
        {
            await ReplyAsync("!scoreboard prints out the players with the most team kills in a list from most to least");
        }

        [Command("tkadd")]
        public async Task AddTeamKillAsync(string playerName, string playerKilled, string mapName)
        {
            // Increment the team kill count for the killer
            if (playerCounts.ContainsKey(playerName))
            {
                playerCounts[playerName]++;
            }
            else
            {
                playerCounts[playerName] = 1;
            }

            // Save the player counts to the JSON file
            SaveData();

            // Generate the current time and date
            var currentTime = DateTime.Now;

            // Format the information
            var tkInfo = $"{currentTime}" +
                         $"{Environment.NewLine}  Killer: {playerName}" +
                         $"{Environment.NewLine}  Who Died: {playerKilled}" +
                         $"{Environment.NewLine}  Map: {mapName}";

            try
            {
                // Save the information to the TeamKillList.txt file
                File.AppendAllText("TeamKillList.txt", tkInfo + Environment.NewLine);

                // Add the team kill information to the dictionary
                if (teamKillList.ContainsKey(playerName))
                {
                    teamKillList[playerName].Add(tkInfo);
                }
                else
                {
                    teamKillList[playerName] = new List<string> { tkInfo };
                }

                // Reply with a success message
                await ReplyAsync($"Team kill added successfully:{Environment.NewLine}{tkInfo}");
            }
            catch (Exception ex)
            {
                // Reply with an error message if something goes wrong
                await ReplyAsync($"Error adding team kill: {ex.Message}");
            }
        }

        [Command("tklist")]
        public async Task ListTeamKillsAsync()
        {
            try
            {
                // Read the contents of the TeamKillList.txt file
                var teamKillList = File.ReadAllLines("TeamKillList.txt");

                // Check if there are team kills recorded
                if (teamKillList.Length > 0)
                {
                    // Format the team kill information into a list
                    var formattedList = new StringBuilder();
                    var currentTeamKill = new List<string>();
                    var currentTimestamp = "";

                    for (int i = 0; i < teamKillList.Length; i++)
                    {
                        // Check if the line starts with a timestamp
                        if (DateTime.TryParse(teamKillList[i], out _))
                        {
                            // If there was a previous team kill, append it to the formatted list
                            if (currentTeamKill.Count > 0)
                            {
                                formattedList.AppendLine($"{currentTimestamp}{Environment.NewLine}{string.Join(Environment.NewLine, currentTeamKill)}");
                                currentTeamKill.Clear();
                            }

                            // Update the current timestamp
                            currentTimestamp = $"{i + 1}. {teamKillList[i]}";
                        }
                        else
                        {
                            // Add non-timestamp lines to the current team kill
                            currentTeamKill.Add($"  {teamKillList[i]}");
                        }
                    }

                    // If there was a team kill at the end, append it to the formatted list
                    if (currentTeamKill.Count > 0)
                    {
                        formattedList.AppendLine($"{currentTimestamp}{Environment.NewLine}{string.Join(Environment.NewLine, currentTeamKill)}");
                    }

                    // Send the formatted list to Discord
                    await ReplyAsync($"Team Kill List:{Environment.NewLine}{formattedList.ToString()}");
                }
                else
                {
                    // Reply with a message indicating no team kills recorded
                    await ReplyAsync("No team kills recorded.");
                }
            }
            catch (Exception ex)
            {
                // Reply with an error message if something goes wrong
                await ReplyAsync($"Error listing team kills: {ex.Message}");
            }
        }


        [Command("tkremove")]
        public async Task RemoveTeamKillAsync(int teamKillNumber)
        {
            try
            {
                // Read the contents of the TeamKillList.txt file
                var teamKillList = File.ReadAllLines("TeamKillList.txt").ToList();

                // Check if there are team kills recorded
                if (teamKillList.Count > 0 && teamKillNumber >= 1 && teamKillNumber <= teamKillList.Count)
                {
                    // Identify the line number corresponding to the team kill number
                    var lineToRemove = (teamKillNumber - 1) * 4;

                    // Extract the information of the team kill to be removed
                    var removedTeamKill = string.Join(Environment.NewLine, teamKillList.GetRange(lineToRemove, 4));

                    // Remove the team kill from the list
                    teamKillList.RemoveRange(lineToRemove, 4);

                    // Update player counts based on the removed team kill
                    var match = Regex.Match(removedTeamKill, @"Killer: (\w+)");
                    if (match.Success)
                    {
                        var playerName = match.Groups[1].Value;
                        if (playerCounts.ContainsKey(playerName))
                        {
                            playerCounts[playerName]--;
                            if (playerCounts[playerName] <= 0)
                            {
                                // Remove the player if the count is zero or negative
                                playerCounts.Remove(playerName);
                            }
                        }
                    }

                    // Save the updated data to the JSON file and TeamKillList.txt
                    SaveData();
                    File.WriteAllLines("TeamKillList.txt", teamKillList);

                    // Reply with a success message
                    await ReplyAsync($"Team kill number {teamKillNumber} removed successfully:{Environment.NewLine}{removedTeamKill}");
                }
                else
                {
                    // Reply with a message indicating an invalid team kill number or no team kills recorded
                    await ReplyAsync($"Invalid team kill number or no recorded team kills for the specified number.");
                }
            }
            catch (Exception ex)
            {
                // Reply with an error message if something goes wrong
                await ReplyAsync($"Error removing team kill: {ex.Message}");
            }
        }


        [Command("scoreboard")]
        public async Task MostTeamKillsAsync()
        {
            try
            {
                // Order players by the number of team kills in descending order
                var orderedPlayers = playerCounts
                    .OrderByDescending(pair => pair.Value)
                    .ToList();

                // Format the information
                var mostKillsInfo = new StringBuilder();
                mostKillsInfo.AppendLine("Players with the most team kills:");

                for (int i = 0; i < orderedPlayers.Count; i++)
                {
                    mostKillsInfo.AppendLine($"{i + 1}. {orderedPlayers[i].Key}, Team Kills: {orderedPlayers[i].Value}");
                }

                // Send the formatted information to Discord
                await ReplyAsync(mostKillsInfo.ToString());
            }
            catch (Exception ex)
            {
                // Reply with an error message if something goes wrong
                await ReplyAsync($"Error getting players with the most team kills: {ex.Message}");
            }
        }

    }

    // Additional class to store data in JSON format
    public class TKModuleData
    {
        public Dictionary<string, List<string>> TeamKillList { get; set; }
        public Dictionary<string, int> PlayerCounts { get; set; }
    }
}

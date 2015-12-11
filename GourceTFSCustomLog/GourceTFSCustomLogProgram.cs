using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace GourceTFSCustomLog
{
    public class GourceTFSCustomLogProgram
    {
        private static DateTime unixTimeZero = new DateTime(1970, 1, 1);

        public static void Main(string[] args)
        {
            Console.WriteLine("Hello there! Welcome to the custom log generator for gource from TFS/VSO!\nWe need some information first...");

            # region tfs web url
            Console.WriteLine("\n\nWhat is the web url of the TFS/VSO instance?");
            Console.WriteLine("HINT: \"DefaultCollection/\" is usually at the end:");
            Uri tfsUri = null;
            var tfsUrl = string.Empty;
            var tfsUrlCount = 0;
            while (string.IsNullOrEmpty(tfsUrl))
            {
                tfsUrlCount++;
                tfsUrl = Console.ReadLine();

                if (string.IsNullOrEmpty(tfsUrl))
                {
                    if (tfsUrlCount == 10)
                    {
                        Console.WriteLine("No web url of the TFS/VSO instance entered for ten times, press any key to exit.");
                        Console.ReadKey();
                        return;
                    }
                    else
                    {
                        Console.WriteLine("How do you get to the TFS/VSO instance via a web browser including the \"DefaultCollection/\"?");
                    }
                }
                else
                {
                    try
                    {
                        tfsUri = new Uri(tfsUrl);
                    }
                    catch
                    {
                        if (tfsUrlCount == 10)
                        {
                            Console.WriteLine("\n\nThe provided web url is not valid!!");
                            Console.ReadKey();
                            return;
                        }
                        else
                        {
                            Console.WriteLine("The provided web url is not valid, please try again.");
                            tfsUrl = string.Empty;
                        }
                    }
                }
            }
            # endregion tfs web url

            # region Username and Password
            //The username and password don't need to be entered if they're giving us a local folder.

            //Retrieve a username, allow them to enter a blank one for 10 times and after that
            //stop letting them and exit
            //Console.WriteLine("Now, what is your username:");
            //var username = string.Empty;
            //var usernameCount = 0;
            //while (string.IsNullOrEmpty(username))
            //{
            //    usernameCount++;
            //    username = Console.ReadLine();

            //    if (string.IsNullOrEmpty(username))
            //    {
            //        if (usernameCount == 10)
            //        {
            //            Console.WriteLine("No username entered for ten times, press any key to exit.");
            //            Console.ReadKey();
            //            return;
            //        }
            //        else
            //        {
            //            Console.WriteLine("What is your username?");
            //        }
            //    }
            //}

            //Retrieve a password, allow them to enter a blank one for 10 times and after that
            //stop letting them and exit
            //Console.WriteLine("\n\nWhat about your password:");
            //var password = string.Empty;
            //var passwordCount = 0;
            //while (string.IsNullOrEmpty(password))
            //{
            //    passwordCount++;
            //    password = Console.ReadLine();

            //    if (string.IsNullOrEmpty(password))
            //    {
            //        if (passwordCount == 10)
            //        {
            //            Console.WriteLine("No password entered for ten times, press any key to exit.");
            //            Console.ReadKey();
            //            return;
            //        }
            //        else
            //        {
            //            Console.WriteLine("What is your password?");
            //        }
            //    }
            //}

            //NetworkCredential credential = new NetworkCredential(username, password);
            
            //tfs.EnsureAuthenticated();

            //if (!tfs.HasAuthenticated)
            //{
            //    Console.WriteLine("Authentication failed. Press any key to continue.");
            //    Console.ReadKey();
            //    return;
            //}

            # endregion Username and Password

            # region local path
            Console.WriteLine("\n\nEnter the LOCAL folder path which contains the TFS/VSO project you want:");
            var localPath = string.Empty;
            var localPathCount = 0;
            while (string.IsNullOrEmpty(localPath))
            {
                localPathCount++;
                localPath = Console.ReadLine(); //C:\vcg vso\Value Discovery Workshop\Portal\DEV

                if (string.IsNullOrEmpty(localPath))
                {
                    if (localPathCount == 10)
                    {
                        Console.WriteLine("No LOCAL folder path which contains the TFS/VSO project entered for ten times, press any key to exit.");
                        Console.ReadKey();
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Where is the LOCAL folder path which contains the TFS/VSO project?");
                    }
                }
                else
                {
                    if (!Directory.Exists(localPath))
                    {
                        Console.WriteLine("That directory does not exist, please try again:");
                        localPath = string.Empty;
                    }
                }
            }
            # endregion local path

            # region saved log location
            Console.WriteLine("\nWhat directory should we save the custom log file to?");
            var directoryForLog = string.Empty;
            var directoryForLogCount = 0;
            while (string.IsNullOrEmpty(directoryForLog))
            {
                directoryForLogCount++;
                directoryForLog = Console.ReadLine();//C:\temp

                if (string.IsNullOrEmpty(directoryForLog))
                {
                    if (directoryForLogCount == 10)
                    {
                        Console.WriteLine("No directory entered to save the custom log to for ten times, press any key to exit.");
                        Console.ReadKey();
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Where do you want the custom log saved?");
                    }
                }
                else
                {
                    if (!Directory.Exists(directoryForLog))
                    {
                        Console.WriteLine("That directory does not exist, please try again:");
                        directoryForLog = string.Empty;
                    }
                }
            }

            var now = DateTime.Now;
            var logPath = string.Format(@"{0}\{1}-{2}-{3}_{4}-{5}-{6}.txt", directoryForLog, now.Month, now.Day, now.Year, now.Hour, now.Minute, now.Second);
            # endregion saved log location

            # region include packages
            Console.WriteLine("\nFinally, should we include the packages folder (assuming your project has one and defaults to true). Y/N");
            var includePackagesText = Console.ReadLine();
            var includePackages = true;
            if (!string.IsNullOrEmpty(includePackagesText))
            {
                includePackages = includePackagesText.ToLower().Equals("yes");
            }
            # endregion include packages

            # region read from source
            Console.WriteLine("\n\nStarting the query and writing the log.");

            var tfs = new TfsTeamProjectCollection(tfsUri);
            var versionControl = tfs.GetService<VersionControlServer>();
            var queryParams = new QueryHistoryParameters(localPath, RecursionType.Full)
            {
                IncludeChanges = true,
                IncludeDownloadInfo = false,
                SortAscending = true
            };

            using (StreamWriter file = new StreamWriter(logPath))
            {
                foreach (var changeSet in versionControl.QueryHistory(queryParams))
                {
                    var s = changeSet.ArtifactUri;
                    double unixTime = (int)(changeSet.CreationDate - unixTimeZero.ToLocalTime()).TotalSeconds;

                    foreach (var change in changeSet.Changes)
                    {
                        var changeTypeCode = GetChangeType(change.ChangeType);
                        if (string.IsNullOrEmpty(changeTypeCode)) continue;

                        var fileName = ConvertFileExtensionToLowerCase(change.Item.ServerItem);

                        if (fileName.Contains("/packages") && !includePackages) continue;

                        file.WriteLine(string.Format("{0}|{1}|{2}|{3}", unixTime, changeSet.OwnerDisplayName, changeTypeCode, fileName));
                    }
                }
            }
            # endregion read from source

            Console.WriteLine(string.Format("Done! Your custom log is now located: {0}", logPath));
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey();
        }

        private static string GetChangeType(ChangeType changeType)
        {
            if ((changeType & ChangeType.Add) == ChangeType.Add)
                return "A";

            if ((changeType & ChangeType.Branch) == ChangeType.Branch)
                return "A";

            if ((changeType & ChangeType.Merge) == ChangeType.Merge)
                return "A";

            if ((changeType & ChangeType.Undelete) == ChangeType.Undelete)
                return "A";

            if ((changeType & ChangeType.Edit) == ChangeType.Edit)
                return "M";

            if ((changeType & ChangeType.Encoding) == ChangeType.Encoding)
                return "M";

            if ((changeType & ChangeType.Lock) == ChangeType.Lock)
                return "M";

            if ((changeType & ChangeType.Property) == ChangeType.Property)
                return "M";

            if ((changeType & ChangeType.Rename) == ChangeType.Rename)
                return "M";

            if ((changeType & ChangeType.SourceRename) == ChangeType.SourceRename)
                return "M";

            if ((changeType & ChangeType.Delete) == ChangeType.Delete)
                return "D";

            if ((changeType & ChangeType.Rollback) == ChangeType.Rollback)
                return "D";

            Console.WriteLine("Missing: " + changeType.ToString());
            return string.Empty;
        }

        private static string ConvertFileExtensionToLowerCase(string fileName)
        {
            var slashIndex = Math.Max(fileName.LastIndexOf('/'), fileName.LastIndexOf('\\'));
            var pointIndex = fileName.LastIndexOf('.');
            if (pointIndex < slashIndex || pointIndex < 1 || pointIndex >= fileName.Length - 1)
                return fileName;

            return fileName.Substring(0, pointIndex + 1) + fileName.Substring(pointIndex + 1, fileName.Length - (pointIndex + 1)).ToLowerInvariant();
        }
    }
}

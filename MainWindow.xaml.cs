﻿using System;
using System.Windows;

using System.IO;
using System.Diagnostics;

using LibGit2Sharp;
using LibGit2Sharp.Handlers;
using NLog;

namespace dataSync
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    ///
    public partial class MainWindow : Window
    {
        private static string thefile = Properties.Settings.Default.thefile;
        private static string dbfile = Properties.Settings.Default.dbfile;
        private static string repoUri = Properties.Settings.Default.repoUri;
        private static string user = Properties.Settings.Default.user;
        private static string email = Properties.Settings.Default.email;

        private static string rootPath {
            get
            {
                string[] path = Properties.Settings.Default.path.Split(';');

                string validPath = "";
                foreach (string p in path)
                {
                    if (Directory.Exists(p))
                    {
                        validPath = p;
                    }
                }
                return validPath;
            }
        }

        private static string repoPath = Path.Combine(rootPath, Properties.Settings.Default.repoDir);
        private static string thefilePath = Path.Combine(repoPath, thefile);
        private static string dbfilePath = Path.Combine(repoPath, dbfile);
        private static string appPath = Path.Combine(rootPath, Properties.Settings.Default.app);

        public static Identity id = new Identity(user, email);
        private static CredentialsHandler credential = new CredentialsHandler(
        (url, usernameFromUrl, types) =>
            new UsernamePasswordCredentials()
            {
                Username = Properties.Settings.Default.gitUsername,
                Password = Properties.Settings.Default.gitPasswd
            });
        private static Logger logger = LogManager.GetLogger("fileLogger");

        private Repository repo;

        private string prepareRepo()
        {
            prompt.Text = "准备数据。。。"; 
            try
            {
                logger.Info("Prepare repo object");
                repo = new Repository(repoPath);
            }
            catch(Exception e)
            {
                logger.Error("Prepare repo failed: " + e.Message);
                return e.Message;
            }
            return "";
        }
        private string pull()
        {
            prompt.Text = "下载数据。。。";
            PullOptions options = new PullOptions();
            options.FetchOptions = new FetchOptions();
            options.FetchOptions.CredentialsProvider = credential;
            try
            {
                logger.Info("Pull from remote");
                var result = Commands.Pull(repo, new Signature(id, DateTimeOffset.Now), options);
                logger.Info(result.Status);
            }
            catch (Exception e)
            {
                logger.Error("Pull failed: " + e.Message);
                return e.Message;
            }
            return "";
        }
        private string push()
        {
            prompt.Text = "上传数据。。。";
            PushOptions options = new PushOptions();
            options.CredentialsProvider = credential;
            try
            {
                logger.Info("Push to remote");
                repo.Network.Push(repo.Head, options);
            }
            catch (Exception e)
            {
                logger.Error("Push failed: " + e.Message);
                return e.Message;
            }
            return "";
        }

        private string commitChange(string status)
        {
            prompt.Text = "更改数据状态。。。";
            try
            {
                logger.Info("Write a to the file");
                StreamWriter thefileWriter = File.AppendText(thefilePath);
                thefileWriter.Write("a");
                thefileWriter.Flush();
                thefileWriter.Close();

                logger.Info("Add files to git index");
                repo.Index.Add(thefile);
                repo.Index.Add(dbfile);
                repo.Index.Write();

                logger.Info("Commit message");
                repo.Commit(status, new Signature(id, DateTimeOffset.Now), new Signature(id, DateTimeOffset.Now));
            }
            catch (Exception e)
            {
                logger.Error("Push failed: " + e.Message);
                return e.Message;
            }
            return "";
        }
        private string dataAvailablity()
        {
            prompt.Text = "检查数据可用性";
            logger.Info("Checking data availablity");
            string lastAuthor = repo.Head.Tip.Author.Name;
            string lastCommit = repo.Head.Tip.Message;
            if (lastAuthor != user && lastCommit.Contains("lock"))
            {
                string msg = lastAuthor + " 正在使用数据文件";
                logger.Error(msg);
                return msg;
            }
            return "";
        }

        private string launch()
        {
            prompt.Text = "启动软件。。。";
            ProcessStartInfo brSystem = new ProcessStartInfo();
            brSystem.FileName = appPath;
            brSystem.WorkingDirectory = rootPath;
            try
            {
                logger.Info("Launch the software");
                Process.Start(brSystem).WaitForExit();
                logger.Info("The software is exited");
            }
            catch (Exception e)
            {
                logger.Error("Launch software error: " + e.Message);
                return e.Message;
            }
            return "";
        }

        private void errorCheck(string errorM)
        {
            if (errorM != "")
            {
                MessageBox.Show(errorM, "错误", MessageBoxButton.OK);
                Environment.Exit(0);
            }
        }

        public MainWindow()
        {
            InitializeComponent();
            logger.Info("");
            logger.Info("=======================");
            logger.Info("=======================");
            logger.Info("=======================");
            logger.Info("=======================");
            logger.Info("Program started");

            errorCheck(prepareRepo());
            errorCheck(pull());
            errorCheck(dataAvailablity());
            errorCheck(push());
            errorCheck(commitChange("lock"));
            errorCheck(launch());
            errorCheck(commitChange("release"));
            errorCheck(push());
        }
    }
}

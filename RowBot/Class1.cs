using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AshitaAPI;
using AshitaAPI.Classes;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.IO;
using System.Timers;


namespace RowBot
{
    public class Main : AshitaBase
    {   
        public IMAshitaCore m_AshitaCore;
        public string Installpath;
        public string Configpath;
        public string Command;
        public List<RowBase> instances = new List<RowBase>();
        
        public override bool Load(IntPtr ashitaCore)
        {
            this.m_AshitaCore = new IMAshitaCore(ashitaCore);

            var console = m_AshitaCore.GetConsoleModule();
            var data = m_AshitaCore.GetDataModule();

            this.Installpath = m_AshitaCore.AshitaInstallPath;
            this.Configpath = Installpath + "default_config.txt";

            console.Write("RowBot v0.9.0 by: Apogee");

            data.SendCommand("/echo /start -Resets and starts all configured timers.", 0);
            data.SendCommand("/echo /stop -Stops all configured timers.", 0);
          
            HandleCommand(Command, 1);

            return true;
        }

        public override bool HandleCommand(string strCommand, int nType)
        {
            this.Command = strCommand;
            var data = m_AshitaCore.GetDataModule();

            switch (strCommand)
            {                 
                case "/start":               
                    data.AddChatLine(0, "RowBot has initialized, commencing configured logic sequence.");
                    KillSequence();
                    BootSequence();
                    break;

                case "/stop":
                    data.AddChatLine(0, "Rowbot is now powering down.. logic sequece de-initialized.");
                    KillSequence();
                    break;
            }

            return base.HandleCommand(strCommand, nType);
        }

        public void BootSequence() 
        {  
            Seek_Config();         
            Build_Sequence(Get_Logic()); 
        }

        public void KillSequence() 
        {
            foreach (RowBase instance in instances) 
            {
                instance.Kill();          
            }

            instances.Clear();
        }

        public void Seek_Config()
        {
            if (!File.Exists(Configpath))
            {
                string[] createText = 
                {   
                    "##########################################################################",
                    "#",
                    "# Welcome to Rowbot configuration!",
                    "#",
                    "# Comments start with '#'.",
                    "#",
                    "# ==== IMPORTANT! ====",
                    "#", 
                    "# --Do not start a line with an empty space.",
                    "# --All parameters must start with '--' and end with ':' Example: '--start:'",
                    "# --Spaces are not a problem. Example: '--start:   8  --end:3' This works.",
                    "# --Only numers should be used for start, end, and repeat.", 
                    "# --Decimal numbers work but are not recommended.", 
                    "#",
                    "##########################################################################",
                    "#",
                    "# Example row: --action: /wave --start: 5 --end: 60 --repeat: 10",
                    "#",
                    "# --This example Row will execute the /wave action in 5 seconds and", 
                    "# from then on will execute again every 10 seconds for one minute.",
                    "#",
                    "##########################################################################",
                    "#",
                    "# :::: TIPS ::::",
                    "#",
                    "# --Enter 0 for '--end:' and any positive number for '--repeat' if you",
                    "# wish to repeat your action indefinitely.",
                    "# --Add as many rows as you like!",
                    "# --Experiment and have fun but please remember to use this responsibly.",
                    "#",
                    "##########################################################################",
                    "#---------ADD YOUR ROWBOT COMMAND ROWS BELOW THESE COMMENT LINES---------#",
                    "#------------------------------------------------------------------------#",
                    "#", 
                    "--action: /say Hello, World! --start: 1 --end: 0 --repeat: 0",
                    "#",
                    "--action: /wave --start: 1 --end: 0 --repeat: 0",
                    "#",
                };

                File.WriteAllLines(Configpath, createText);
            }

        }

        public List<string> Get_Logic()
        {
            var data = m_AshitaCore.GetDataModule();
            string commentpattern = "#";

            List<string> rowlist = new List<string>();

            using (StreamReader reader = new StreamReader(Configpath))
            {
                string line;

                while ((line = reader.ReadLine()) != null)
                {
                    Match commentmatch = Regex.Match(line, commentpattern);

                    if (!commentmatch.Success)
                    {
                        rowlist.Add(line);

                    }
                }
            }

            return rowlist;
        }

        public string Get_Match(string input, string pattern)
        {
            Regex _regex = new Regex(pattern);

            Match match = _regex.Match(input);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
            else
            {
                return "None";
            }
        }      

        public void Build_Sequence(List<string> rowlist)
        {
            int rownum = 0;

            string actionpattern = @"(?<=\-\-)(?:action:)(?:\W*)(\/.*?)(?=\-\-)";
            string startpattern = @"(?<=\-\-)(?:start:)(?:\W*)(.*?)(?:\W*)(?=\-\-)";
            string endpattern = @"(?<=\-\-)(?:end:)(?:\W*)(.*?)(?:\W*)(?=\-\-)";
            string repeatpattern = @"(?<=\-\-)(?:repeat:)(?:\W*)(\d+)";

            foreach (string row in rowlist)
            {
                rownum += 1;

                RowBase rowbase = new RowBase(m_AshitaCore, rownum, row);

                rowbase.action = Get_Match(row, actionpattern);
                rowbase.start = Get_Match(row, startpattern);
                rowbase.end = Get_Match(row, endpattern);
                rowbase.repeat = Get_Match(row, repeatpattern);

                instances.Add(rowbase);

                rowbase.Execute_Sequence();
            }

        }

        public class RowBase
        {
            System.Timers.Timer main_timer = new System.Timers.Timer();
            System.Timers.Timer end_timer = new System.Timers.Timer();

            public string action;
            public string start;
            public string end;
            public string repeat;
          
            public int Start { get { return Convert.ToInt32(start) * 1000; } }

            public int End { get { return Convert.ToInt32(end) * 1000; } }

            public int Repeat { get { return Convert.ToInt32(repeat) * 1000; } }

            public IMAshitaCore core;

            public int rownum;
            public string row;      

            public RowBase(IMAshitaCore m_ashitacore, int rownum, string row)
            {
                this.core = m_ashitacore;
                this.rownum = rownum;
                this.row = row;           
            }

            public void Kill()
            {   
                //stop main_timer.
                main_timer.Enabled = false;
                main_timer.Stop();
                main_timer.Close();

                //stop end_timer.
                end_timer.Enabled = false;
                end_timer.Stop();
                end_timer.Close();
            }

            public void Begin()
            {
                main_timer.Enabled = true;
                main_timer.Start();
            }

            public void Execute_Sequence()
            {   
                if (End > 0) 
                {
                   end_timer.Interval = 1000;
                   end_timer.Enabled = true;
                   end_timer.Start();
                }
    
                //if start 'IS' set.
                if (Start !=0)
                {              
                    main_timer.Interval = Start;
                    Begin();             
                }

                else
                {   //if repeat 'IS' set.
                    if (Repeat !=0)
                    {
                        main_timer.Interval = Repeat;
                        Begin();
                    }
                    
                    //if repeat 'IS NOT' set.
                    else
                    {
                        core.GetConsoleModule().Write(row + " @pos " + Convert.ToString(rownum) + " does not have a '--start:' or '--repeat:' parameter set and therefore will not execute.");
                    }

                }

                main_timer.Elapsed += new ElapsedEventHandler(main_timer_Elapsed);
                end_timer.Elapsed += new ElapsedEventHandler(end_timer_Elapsed);
            }

            private void main_timer_Elapsed(object source, System.Timers.ElapsedEventArgs e)
            {         
                    core.GetDataModule().SendCommand(action, 0); 
                
                //if repeat 'IS' set.
                if (Repeat !=0) 
                {
                    //repeat indefinitely..   
                    main_timer.Interval = Repeat;              
                }
                
                //if repeat 'IS NOT' set.
                else 
                {   
                    main_timer.Enabled = false;
                    main_timer.Stop();
                    main_timer.Close();

                    //if end 'IS NOT' set.
                    if (End <= 0) 
                    {
                        core.GetDataModule().SendCommand("/echo " + row + " @pos " + Convert.ToString(rownum) + " The timer for this row has ended and its action and will no longer be executed.", 0);
                    }
                    
                }                                      
                    
            }

            private void end_timer_Elapsed(object source, System.Timers.ElapsedEventArgs e)
            {
                // subtract interval from end parameter for our countdown.
                int interval = Convert.ToInt32(end_timer.Interval) / 1000;       
                int adjend = Convert.ToInt32(this.end) - interval;
                
                //reset the end parameter.
                this.end = Convert.ToString(adjend);
                
                if (End <=0) 
                {
                    Kill(); 
                    core.GetDataModule().SendCommand("/echo " + row + " @pos " + Convert.ToString(rownum) + " The timer for this row has ended and its action and will no longer be executed.", 0);
                }
                         
            }
       
        }
       
    }
}

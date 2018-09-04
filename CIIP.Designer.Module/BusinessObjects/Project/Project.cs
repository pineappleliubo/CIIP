﻿using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using DevExpress.ExpressApp.DC;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using CIIP.Persistent.BaseImpl;
using System.Linq;
using DevExpress.ExpressApp.Model;
using System.IO;

namespace CIIP.ProjectManager
{
    //系统起动后,必须选择一个项目才可以继续.
    //启动按钮:
    //1.先运行生成按project,
    //2.运行StartupFile
    //3.startupFile如何知道生成的文件?
    //生成按钮:生成project,内容包含bo文件
    //起动文件中配置了自动读取dll模块的方法

    [XafDisplayName("项目管理")]
    [DefaultClassOptions]
    public class Project : NameObject
    {
        public static string ApplicationStartupPath { get; set; }
        public Project(Session s) : base(s)
        {
        }

        protected override void OnChanged(string propertyName, object oldValue, object newValue)
        {
            base.OnChanged(propertyName, oldValue, newValue);
            if (propertyName == nameof(Name) || propertyName == nameof(ProjectPath))
            {
                WinProjectPath = Path.Combine(ProjectPath + "", Name + "", "Win");
                WinStartupFile = Path.Combine(WinProjectPath + "",  "CIIP.Client.Win.Exe");
                WebProjectPath = Path.Combine(ProjectPath + "", Name + "", "Web");
            }
        }

        public override void AfterConstruction()
        {
            base.AfterConstruction();
            ProjectPath = ApplicationStartupPath;
            WinStartupFile = ApplicationStartupPath + "\\CIIP.Client.Win.Exe";
        }

        /// <summary>
        /// 是否是本系统生成的
        /// </summary>
        [XafDisplayName("定制生成")]
        [ToolTip("选中时为定系统生成的,否则为存在的dll导入的.")]
        [ModelDefault("AllowEdit", "False")]
        public bool Generated
        {
            get { return GetPropertyValue<bool>(nameof(Generated)); }
            set { SetPropertyValue(nameof(Generated), value); }
        }

        /// <summary>
        /// 在windows下调试时使用哪个起动文件
        /// </summary>
        [XafDisplayName("起动文件")]
        public string WinStartupFile
        {
            get { return GetPropertyValue<string>(nameof(WinStartupFile)); }
            set { SetPropertyValue(nameof(WinStartupFile), value); }
        }
        [XafDisplayName("项目路径")]

        public string WinProjectPath
        {
            get { return GetPropertyValue<string>(nameof(WinProjectPath)); }
            set { SetPropertyValue(nameof(WinProjectPath), value); }
        }
        [XafDisplayName("项目路径")]
        public string WebProjectPath
        {
            get { return GetPropertyValue<string>(nameof(WebProjectPath)); }
            set { SetPropertyValue(nameof(WebProjectPath), value); }
        }


        /// <summary>
        /// 路径:生成dll时保存到如下路径中去.
        /// </summary>
        [XafDisplayName("项目路径")]
        public string ProjectPath
        {
            get { return GetPropertyValue<string>(nameof(ProjectPath)); }
            set { SetPropertyValue(nameof(ProjectPath), value); }
        }

        protected override void OnSaved()
        {
            base.OnSaved();
            //如果启动文件缺少,则复复制.
            var source = Path.Combine(ApplicationStartupPath, @"StartupFile\Win");
            DirectoryInfo fi = new DirectoryInfo(source);
            var files = fi.EnumerateFiles("*.*", SearchOption.AllDirectories);
            if (!File.Exists(WinStartupFile))
            {
                //File.Copy(ApplicationStartupPath)
                DirectoryCopy(source, WinProjectPath, true);
            }
        }

        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }

            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }

    public class ProjectViewController : ViewController
    {
        public ProjectViewController()
        {
            TargetObjectType = typeof(Project);
        }
        protected override void OnActivated()
        {
            base.OnActivated();
            ObjectSpace.Committed += ObjectSpace_Committed;
        }
        
        protected override void OnDeactivated()
        {
            ObjectSpace.Committed -= ObjectSpace_Committed;
            base.OnDeactivated();
        }

        private void ObjectSpace_Committed(object sender, System.EventArgs e)
        {
            Application.MainWindow?.GetController<SwitchProjectControllerBase>()?.CreateProjectItems();
        }        
    }

    public enum CompileAction
    {
        编译运行,
        开始暂停,
        运行,
        编译
    }
    public abstract class SwitchProjectControllerBase : WindowController
    {
        public static Project CurrentProject { get; set; }
        SingleChoiceAction switchProject;
        public SwitchProjectControllerBase()
        {
            TargetWindowType = WindowType.Main;
            switchProject = new SingleChoiceAction(this, "SwitchProject", "项目");
            switchProject.Caption = "当前项目";
            switchProject.Execute += SwitchProject_Execute;
            switchProject.ItemType = SingleChoiceActionItemType.ItemIsOperation;

            var compileProject = new SingleChoiceAction(this, "CompileProject", "项目");
            compileProject.Caption = "生成";
            compileProject.Items.Add(new ChoiceActionItem("生成运行", CompileAction.编译运行));
            compileProject.Items.Add(new ChoiceActionItem("生成运行-开始时暂停", CompileAction.开始暂停));

            compileProject.Items.Add(new ChoiceActionItem("生成项目", CompileAction.编译));
            compileProject.Items.Add(new ChoiceActionItem("运行", CompileAction.运行));
            compileProject.ItemType = SingleChoiceActionItemType.ItemIsOperation;
            compileProject.Execute += CompileProject_Execute;
        }
        protected abstract void Compile(SingleChoiceActionExecuteEventArgs e);
        private void CompileProject_Execute(object sender, SingleChoiceActionExecuteEventArgs e)
        {
            if (CurrentProject == null)
            {
                var msg = new MessageOptions();
                msg.Duration = 3000;
                msg.Message = "当前没有选中的项目!";
                msg.Type = InformationType.Error;
                msg.Win.Caption = "错误";
                msg.Win.Type = WinMessageType.Flyout;
                Application.ShowViewStrategy.ShowMessage(msg);// "", InformationType.Error, 3000, InformationPosition.Left);
                return;
            }
            Compile(e);
        }

        protected override void OnActivated()
        {
            base.OnActivated();
            CreateProjectItems();

        }

        protected abstract void ShowMessage(string msg);

        public void CreateProjectItems()
        {
            switchProject.Items.Clear();
            var os = Application.CreateObjectSpace();
            var projects = os.GetObjectsQuery<Project>().ToList();
            if (projects.Count == 0)
            {
                ShowMessage("当前系统还没有默认项目,请建立一个默认项目!");
                var obj = os.CreateObject<Project>();
                var view = Application.CreateDetailView(os, obj);
                Application.ShowViewStrategy.ShowViewInPopupWindow(view, okButtonCaption: "创建项目", cancelDelegate: () => { Application.Exit(); }, cancelButtonCaption: "退出系统");
                projects.Add(obj);
            }

            foreach (var item in projects)
            {
                switchProject.Items.Add(new ChoiceActionItem(item.Name, item));
            }

            switchProject.SelectedItem = switchProject.Items.FirstOrDefault();
            SwitchProjectCore(switchProject.SelectedItem?.Data as Project);
        }

        private void SwitchProject_Execute(object sender, SingleChoiceActionExecuteEventArgs e)
        {
            SwitchProjectCore(e.SelectedChoiceActionItem?.Data as Project);
        }

        private void SwitchProjectCore(Project project)
        {
            CurrentProject = project;
            if (CurrentProject != null)
            {
                switchProject.Caption = CurrentProject.Name;
            }
        }
    }
}
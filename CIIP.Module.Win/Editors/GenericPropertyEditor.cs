﻿using System;
using DevExpress.ExpressApp.Editors;
using DevExpress.ExpressApp.Model;
using DevExpress.ExpressApp.Win.Editors;
using DevExpress.XtraEditors;
using DevExpress.XtraEditors.Repository;
using CIIP.Module.BusinessObjects.SYS;
using System.Linq;
using DevExpress.Xpo;
using System.Collections;
using DevExpress.Utils;
using DevExpress.ExpressApp;
using System.Windows.Forms;

namespace CIIP.Module.Win.Editors
{
    /// <summary>
    /// 仅用于实现基类+接口控件
    /// </summary>
    [PropertyEditor(typeof(object), false)]
    public class GenericPropertyEditor : DXPropertyEditor,IComplexViewItem
    {
        public GenericPropertyEditor(Type objectType, IModelMemberViewItem model)
            : base(objectType, model)
        {
            //ControlBindingProperty = "EditValue";
        }

        protected override void ReadValueCore()
        {
            if (Control != null)
            {
                if (CurrentObject != null)
                {
                    var values = (this.PropertyValue as IEnumerable).OfType<GenericParameterDefine>().Select(x => x.Name);// as System.ComponentModel.IBindingList;
                    control.EditValue = string.Join(control.Properties.EditValueSeparatorChar.ToString(), values);
                }
            }
        }

        protected override void WriteValueCore()
        {
            //base.WriteValueCore();
            //if (control != null)
            //{
            //    if (CurrentObject != null)
            //    {
            //        var attachmentList = this.PropertyValue as System.ComponentModel.IBindingList;
            //        var result = String.Empty;
            //        foreach (TokenEditToken item in control.Properties.Tokens)
            //        {
            //            var fileData = attachmentList.AddNew() as DevExpress.Persistent.Base.IFileData;
            //            var archivo = (string)item.Value;
            //            fileData.LoadFromStream(System.IO.Path.GetFileName(archivo), System.IO.File.OpenRead(archivo));
            //        }
            //    }
            //}
        }
        BusinessObjectBase _tokenService;
        BusinessObjectBase tokenService
        {
            get
            {
                if (_tokenService == null)
                {
                    _tokenService = CurrentObject as BusinessObjectBase;
                    if(_tokenService == null)
                    {
                        throw new Exception("CurrentObject must be is a BusinessObjectBase!");
                    }
                }
                return _tokenService;
            }
        }

        TokenEdit control;
        protected override object CreateControlCore()
        {
            control = new TokenEdit();
            return control;
        }
        private void Control_ValidateToken(object sender, TokenEditValidateTokenEventArgs e)
        {
            e.IsValid = true;//DocFormatRegex.IsMatch(e.Description);
        }
        protected override void SetupRepositoryItem(RepositoryItem item)
        {
            base.SetupRepositoryItem(item);
            this.AllowEdit.RemoveItem("MemberIsNotReadOnly");
            RepositoryItemTokenEdit i = item as RepositoryItemTokenEdit;
            i.EditMode = TokenEditMode.Manual;
            i.ShowDropDown = true;
            i.DropDownShowMode = TokenEditDropDownShowMode.Default;
            i.EditValueSeparatorChar = ',';
            i.PopupPanelOptions.ShowMode = TokenEditPopupPanelShowMode.Default;
            i.PopupPanelOptions.ShowPopupPanel = true;
            i.PopupPanelOptions.Location = TokenEditPopupPanelLocation.Default;
            var flyoutPanel = new FlyoutPanel();
            flyoutPanel.Width = 500;
            flyoutPanel.Height = 100;
            i.PopupPanel = flyoutPanel;
            i.BeforeShowPopupPanel += I_BeforeShowPopupPanel;

            i.EditValueType = TokenEditValueType.String;

            i.ValidateToken += Control_ValidateToken;
            i.TokenAdded += I_TokenAdded;
            i.TokenRemoved += I_TokenRemoved;
            i.Tokens.ListChanged += Tokens_ListChanged;
        }

        private void Tokens_ListChanged(object sender, System.ComponentModel.ListChangedEventArgs e)
        {
            if (e.ListChangedType == System.ComponentModel.ListChangedType.ItemAdded)
            {
                var token = new GenericParameterDefine(tokenService.Session);
                token.Name = (sender as TokenEditTokenCollection)[e.NewIndex].Description;
                tokenService.GenericParameterDefines.Add(token);
                OnValueStored();
            }
        }

        private void I_BeforeShowPopupPanel(object sender, TokenEditBeforeShowPopupPanelEventArgs e)
        {
            var value = tokenService.GenericParameterDefines.FirstOrDefault(x=>x.Name == e.Description);
            if (value != null)
            {
                var view = application.CreateDetailView(os, value, false);
                view.CreateControls();
                var fp = control.Properties.PopupPanel as FlyoutPanel;
                fp.Controls.Clear();
                fp.Controls.Add((Control)view.Control);
            }
        }

        private void I_TokenRemoved(object sender, TokenEditTokenRemovedEventArgs e)
        {
            var token = tokenService.GenericParameterDefines.FirstOrDefault(x => x.Name == e.Token.Description);
            tokenService.GenericParameterDefines.Remove(token);
            OnValueStored();
        }

        private void I_TokenAdded(object sender, TokenEditTokenAddedEventArgs e)
        {

        }

        protected override RepositoryItem CreateRepositoryItem()
        {
            return new RepositoryItemTokenEdit();
        }
        IObjectSpace os;
        XafApplication application;
        public void Setup(IObjectSpace objectSpace, XafApplication application)
        {
            this.os = objectSpace;
            this.application = application;
        }
    }
}
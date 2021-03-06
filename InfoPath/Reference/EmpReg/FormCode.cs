using Microsoft.Office.InfoPath;
using System;
using System.Xml;
using System.Xml.XPath;

namespace EmpReg
{
    public partial class FormCode
    {
        // Member variables are not supported in browser-enabled forms.
        // Instead, write and read these values from the FormState
        // dictionary using code such as the following:
        //
        // private object _memberVariable
        // {
        //     get
        //     {
        //         return FormState["_memberVariable"];
        //     }
        //     set
        //     {
        //         FormState["_memberVariable"] = value;
        //     }
        // }

        // NOTE: The following procedure is required by Microsoft InfoPath.
        // It can be modified using Microsoft InfoPath.
        public void InternalStartup()
        {
            EventManager.FormEvents.Loading += new LoadingEventHandler(FormEvents_Loading);
            EventManager.XmlEvents["/my:EmployeeForm/my:ddlCountry"].Changed += new XmlChangedEventHandler(ddlCountry_Changed);
            EventManager.XmlEvents["/my:EmployeeForm/my:ddlState"].Changed += new XmlChangedEventHandler(ddlState_Changed);
            EventManager.FormEvents.Submit += new SubmitEventHandler(FormEvents_Submit);
            ((ButtonEvent)EventManager.ControlEvents["btnGetNameAndManager"]).Clicked += new ClickedEventHandler(btnGetNameAndManager_Clicked);
            
        }

        public void FormEvents_Loading(object sender, LoadingEventArgs e)
        {
            // Write your code here.
            DataConnection connection = this.DataConnections["EmpDesignation"];
            connection.Execute();

            DataConnection connectionEmpCountry = this.DataConnections["EmpCountry"];
            connectionEmpCountry.Execute();

            //XPathNavigator form = this.MainDataSource.CreateNavigator();
            //form.SelectSingleNode("/my:EmployeeForm/my:txtUserID", NamespaceManager).SetValue(this.User.LoginName);            
        }

        public void ddlCountry_Changed(object sender, XmlEventArgs e)
        {
            XPathNavigator form = this.MainDataSource.CreateNavigator();
            FileQueryConnection conState = (FileQueryConnection)DataConnections["restwebserviceState"];
            string stateQuery = conState.FileLocation;
            if (stateQuery.IndexOf("?") > 0)
            {
                stateQuery = stateQuery.Substring(0, stateQuery.IndexOf("?"));
            }
            conState.FileLocation = stateQuery + "?$filter=CountryId eq " + e.NewValue + "&noredirect=true";
            conState.Execute();
        }

        public void ddlState_Changed(object sender, XmlEventArgs e)
        {
            XPathNavigator form = this.MainDataSource.CreateNavigator();
            XPathNavigator ddlstateNode = form.SelectSingleNode("/my:EmployeeForm/my:ddlState", NamespaceManager);
            this.DataSources["EmpCity"].CreateNavigator().SelectSingleNode("/dfs:myFields/dfs:queryFields/q:SharePointListItem_RW/q:State", NamespaceManager).SetValue(ddlstateNode.Value);
            this.DataSources["EmpCity"].QueryConnection.Execute();

        }

        public void FormEvents_Submit(object sender, SubmitEventArgs e)
        {
            // If the submit operation is successful, set
            // e.CancelableArgs.Cancel = false;
            // Write your code here.
            try
            {

                FileSubmitConnection conSubmit = (FileSubmitConnection)DataConnections["SharePoint Library Submit"];
                conSubmit.Execute();
                e.CancelableArgs.Cancel = false;
            }
            catch (Exception ex)
            {
                e.CancelableArgs.Message = ex.Message;
                e.CancelableArgs.Cancel = true;
            }

            this.ViewInfos.SwitchView("Thanks");

        }

        public void btnGetNameAndManager_Clicked(object sender, ClickedEventArgs e)
        {
            string firstname = string.Empty;
            string lastname = string.Empty;
            string manager = string.Empty;

            XPathNavigator form = this.MainDataSource.CreateNavigator();
            form.SelectSingleNode("/my:EmployeeForm/my:txtError", NamespaceManager).SetValue("");
           
            try
            {                
                string userID = form.SelectSingleNode("/my:EmployeeForm/my:txtUserID", NamespaceManager).Value;
                XPathNavigator profileNav = this.DataSources["GetUserProfileByName"].CreateNavigator();
                profileNav.SelectSingleNode("/dfs:myFields/dfs:queryFields/tns:GetUserProfileByName/tns:AccountName", NamespaceManager).SetValue(userID);
                
                WebServiceConnection webServiceConnection = (WebServiceConnection)this.DataConnections["GetUserProfileByName"];
                webServiceConnection.Execute();

                string profileXPath = "/dfs:myFields/dfs:dataFields/tns:GetUserProfileByNameResponse/tns:GetUserProfileByNameResult/tns:PropertyData/tns:Values/tns:ValueData/tns:Value[../../../tns:Name = '{0}']";

                if (profileNav.SelectSingleNode(string.Format(profileXPath, "FirstName"), NamespaceManager) != null)
                {
                   firstname = profileNav.SelectSingleNode(string.Format(profileXPath, "FirstName"), NamespaceManager).Value;
                }
                if (profileNav.SelectSingleNode(string.Format(profileXPath, "LastName"), NamespaceManager) != null)
                {
                    lastname = profileNav.SelectSingleNode(string.Format(profileXPath, "LastName"), NamespaceManager).Value;
                }
                if (profileNav.SelectSingleNode(string.Format(profileXPath, "Manager"), NamespaceManager) != null)
                {
                    manager = profileNav.SelectSingleNode(string.Format(profileXPath, "Manager"), NamespaceManager).Value;
                }
                
                string userName = string.Format("{0} {1}", firstname, lastname);
                                
                form.SelectSingleNode("/my:EmployeeForm/my:txtName", NamespaceManager).SetValue(userName);
                form.SelectSingleNode("/my:EmployeeForm/my:txtManager", NamespaceManager).SetValue(manager);
                
            }
            catch (Exception ex)
            {
                form.SelectSingleNode("/my:EmployeeForm/my:txtError", NamespaceManager).SetValue(ex.Message);
            }
        }
        
    }
}

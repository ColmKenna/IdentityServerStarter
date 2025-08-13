using System.Text;

namespace IdentityServerAspNetIdentity.Tests;

public static class NavExpectedContent
{
    public static string OuterDiv(string id, string innerHtml)
    {
        return @$"<div class=""nav nav-tabs flex-sm-column"" id=""${id}"" role=""tablist""> {innerHtml} </div>";
    }

    public static string NavButtons(List<string> stringList)
    {
        var baseTemplate = @"
            <button class=""nav-link {0}"" id=""{1}_button"" data-bs-toggle=""tab""
            data-bs-target=""#{1}"" type=""button"" role=""tab"" aria-controls=""nav-home""
            aria-selected=""{2}"" tabindex=""-1"">{3}</button>
    ";

        StringBuilder navHtml = new StringBuilder();
        navHtml.Append("<nav>\n<div class=\"nav nav-tabs\" id=\"nav-tab\" role=\"tablist\">\n");

        for (int i = 0; i < stringList.Count; i++)
        {
            var name = stringList[i];
            var active = i == 0 ? "active" : "";
            var selected = i == 0 ? "true" : "false";
            navHtml.Append(string.Format(baseTemplate, active, name, selected));
        }

        navHtml.Append("\n</div>\n</nav>");
        return navHtml.ToString();
    }

    public static string TabContent(IDictionary<string, string> tabContent)
    {
        var baseTemplate = @"<div class=""tab-pane fade "" id=""{0}"" name=""{1}"" role=""tabpanel""
        aria-labelledby=""{2}-tab"">
                {3}
            </div>";
        StringBuilder tabHtml = new StringBuilder();
        foreach (var (key, value) in tabContent)
        {
            tabHtml.Append(string.Format(baseTemplate, key, key, key, value));
        }

        return tabHtml.ToString();
    }
}


public static class ListExpectedContent
{
    
    public static string AddNewItem(string id, string listName, string functionCall)
    {
        string html = $@"
    <li class='list-group-item d-flex'>
        <input type='text' id='{id}' class='col-10'
            placeholder='Add new item'></input>
        <button type='button' class='col-2 btn btn-outline-primary float-right'
            onclick='addButtonPressed(this, ""{id}"", ""{listName}"", ""{functionCall}"")'><i
                class='bi bi-plus'></i></button>
    </li>";

        return html;
    }

    public static string ListItem(string innerHtml)
    {
        string htmlString = $@"<div class=""form-group row"">
                                    {innerHtml}
                                </div>";

        return htmlString;
    }
    
    public static string ListItem(string arrayName, string itemValue, string itemId)
    {
        string htmlString = $@"
 <div class=""col-sm-6 col-xs-12 col-lg-4"">
    <input class=""form-check-input"" type=""checkbox"" name=""{arrayName}[]"" value=""{itemValue}"" id=""{itemId}"" checked></input>
    <label class=""form-check-label"" for=""{itemId}"">
        {itemValue}
    </label>
</div>";

        return htmlString;
    }
    
    public static string GenerateList(IList<string> values, string arrayName)
    {
        StringBuilder htmlString = new StringBuilder();
        htmlString.Append(AddNewItem(Guid.NewGuid().ToString(), arrayName, "addButtonPressed"));
        foreach (var value in values)
        {
            htmlString.Append(ListItem(arrayName, value, Guid.NewGuid().ToString()));
        }

        return ListItem(htmlString.ToString());
    }
    
    
}


public static class KeyValueListExpectedContent
{
    
    
    public static string Generate(IDictionary<string,string> values, string arrayName)
    {
        StringBuilder htmlString = new StringBuilder();
        htmlString.Append(AddNewItem(Guid.NewGuid().ToString(), arrayName));
        for (int i = 0; i < values.Count; i++)
        {
            var key = values.ElementAt(i).Key;
            var value = values.ElementAt(i).Value;
            htmlString.Append(ListItem(arrayName,i, key, value));
        }
        
        
        return ListItem(htmlString.ToString(), arrayName);
    }
    
    public static string AddNewItem(string properties, string propertyContainer)
    {
        string id = properties.Replace('.', '_');

        string html = $@"
    <li>
        <div id='add{id}' class='list-group-item d-flex'>
            <input class='col-2' type='text' id='new{id}Key' placeholder='Enter Key'>
            <input class='col-8' type='text' id='new{id}Value' placeholder='Enter Value'>
            <button class='col-2 btn btn-outline-primary float-right' type='button' onclick='AddKeyValueItem(""{propertyContainer}"",""{properties}"", ""new{id}Key"", ""new{id}Value"") '>
                <i class='bi bi-plus'></i>
            </button>
        </div>
    </li>";

        return html;
    }

    public static string ListItem(string innerHtml, string id)
    {
        string htmlString = $@"<ul class=""list-group"" id=""{id}"">
                                    {innerHtml}
                                </div>";
        return htmlString;
    }
    
    public static string ListItem(string KeyValueListName, int index, string key, string val)
    {
        string html = $@"
        <li class=""list-group-item d-flex"">
            <label class=""control-label col-2"" for=""{KeyValueListName}{index}__Key"">{key}</label>
            <input class=""d-none"" type=""text"" value=""{key}"" id=""{KeyValueListName}{index}__Key"" name=""{KeyValueListName}[{index}].Key"">
            <input class=""col-8"" type=""text"" value=""{val}"" id=""{KeyValueListName}{index}__Value"" name=""{KeyValueListName}[{index}].Value"">
            <span class=""field-validation-valid"" data-valmsg-for=""{KeyValueListName}[{index}].Value"" data-valmsg-replace=""true""></span>
            <button class=""col-2 btn btn-outline-danger delete float-right"" onclick=""removeItem('propertyContainer','{KeyValueListName}',{index})"">
                <i class=""bi bi-x""></i>
            </button>
        </li>";
        return html;
    }
}





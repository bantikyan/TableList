using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using System.Web.Mvc.Html;

namespace Zetalex.TableList.Mvc
{
    public static class HtmlHelpers
    {
        public static MvcHtmlString TableListFor<TModel, TProperty>(this HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, bool showHeader = false, bool allowAdd = true)
        {
            return TableListFor(html, expression, null, showHeader: showHeader, allowAdd: allowAdd);
        }

        public static MvcHtmlString TableListFor<TModel, TProperty>(this HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, object htmlAttributes, bool showHeader = false, bool allowAdd = true)
        {
            return TableListFor(html, expression, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes), showHeader: showHeader, allowAdd: allowAdd);
        }

        public static MvcHtmlString TableListFor<TModel, TProperty>(this HtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes, bool showHeader = false, bool allowAdd = true)
        {
            var fieldName = ExpressionHelper.GetExpressionText(expression);
            //var fullBindingName = html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(fieldName);
            //var fieldId = TagBuilder.CreateSanitizedId(fullBindingName);

            var metadata = ModelMetadata.FromLambdaExpression(expression, html.ViewData);
            dynamic model = metadata.Model;
            Type baseType = metadata.ModelType.GetGenericArguments().Single();

            var properties = baseType.GetProperties().ToList();
            var typeMetadata = ModelMetadataProviders.Current.GetMetadataForType(null, baseType);

            var propertyAttributes = new Dictionary<string, IDictionary<string, object>>();
            var formattingAttributes = new Dictionary<string, IDictionary<string, string>>();

            foreach (var prop in properties)
            {
                var propType = GetPropertyType(prop);

                var propertyData = typeMetadata.Properties.Where(x => x.PropertyName == prop.Name).FirstOrDefault();
                var attrs = html.GetUnobtrusiveValidationAttributes(prop.Name, propertyData);

                //attrs.Add("class", "form-control");
                attrs.Add("class", "");
                attrs.Add("placeholder", propertyData.DisplayName ?? prop.Name);
                if (propertyData.IsReadOnly)
                {
                    attrs.Add("readonly", "readonly");
                }

                if (propType == typeof(DateTime))
                {
                    attrs["class"] += " date-picker";
                }

                if (propType == typeof(bool) && Attribute.IsDefined(prop, typeof(TableListRadioButton)))
                {
                    attrs.Add("data-group", "group" + prop.Name);
                }

                propertyAttributes.Add(prop.Name, attrs);

                var fAttrs = new Dictionary<string, string>();
                fAttrs.Add(nameof(propertyData.DisplayName), propertyData.DisplayName);
                fAttrs.Add(nameof(propertyData.DisplayFormatString), propertyData.DisplayFormatString);
                formattingAttributes.Add(prop.Name, fAttrs);
            }

            var table = new TagBuilder("table");

            if (showHeader)
            {
                AppendHead(table, properties, formattingAttributes);
            }

            var tbody = new TagBuilder("tbody");

            if (model != null && model.Count > 0)
            {
                AppendRows(html, tbody, properties, model, fieldName, propertyAttributes, formattingAttributes);
            }

            if (allowAdd)
            {
                AppendLastRow(html, tbody, properties, fieldName, propertyAttributes, formattingAttributes, model != null ? model.Count : 0);
            }

            if (htmlAttributes != null)
            {
                table.MergeAttributes(htmlAttributes);
            }

            if (table.Attributes.ContainsKey("class"))
            {
                table.Attributes["class"] += " table-list-mvc";
            }
            else
            {
                table.Attributes.Add("class", "table-list-mvc");
            }

            table.InnerHtml += tbody;

            return new MvcHtmlString(table.ToString(TagRenderMode.Normal));
        }

        private static void AppendRows(HtmlHelper html, TagBuilder tag, List<PropertyInfo> properties, dynamic items, string fieldName, Dictionary<string, IDictionary<string, object>> propertyAttributes, Dictionary<string, IDictionary<string, string>> formattingAttributes)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var fName = fieldName + "[" + i + "].";

                var tr = new TagBuilder("tr");
                tr.Attributes.Add("class", "table-list-mvc-item-view");
                tr.Attributes.Add("data-item-index", i.ToString());

                var allowModify = (bool)properties.FirstOrDefault(p => p.Name.ToLower() == nameof(TableListItem.TL_AllowModify).ToLower()).GetValue((TableListItem)items[i]);

                foreach (var prop in properties)
                {
                    var isHidden = Attribute.IsDefined(prop, typeof(TableListHiddenInput));
                    if (isHidden)
                    {
                        continue;
                    }

                    var propertyAttributesClone = CloneDictionary(propertyAttributes);
                    if (!allowModify && !propertyAttributesClone[prop.Name].ContainsKey("readonly"))
                    {
                        propertyAttributesClone[prop.Name].Add("readonly", "readonly");
                    }

                    var td = new TagBuilder("td");

                    var propType = GetPropertyType(prop);
                    if (propType == typeof(bool))
                    {
                        td.InnerHtml += html.CheckBox(fName + prop.Name, Convert.ToBoolean(prop.GetValue((TableListItem)items[i])), propertyAttributesClone[prop.Name]);
                    }
                    else
                    {
                        td.InnerHtml += html.TextBox(fName + prop.Name, prop.GetValue((TableListItem)items[i]), formattingAttributes[prop.Name]["DisplayFormatString"], propertyAttributesClone[prop.Name]);
                    }
                    
                    td.InnerHtml += html.ValidationMessage(fName + prop.Name);

                    tr.InnerHtml += td;
                }

                var tdLast = new TagBuilder("td");

                foreach (var prop in properties)
                {
                    var isHidden = Attribute.IsDefined(prop, typeof(TableListHiddenInput));
                    if (!isHidden)
                    {
                        continue;
                    }

                    var val = prop.GetValue((TableListItem)items[i]);
                    tdLast.InnerHtml += html.Hidden(fName + prop.Name, val);

                    if (prop.Name == nameof(TableListItem.TL_AllowDelete) && (bool)val)
                    {
                        var a = new TagBuilder("a");
                        a.Attributes.Add("class", "table-list-mvc-item-delete");
                        a.Attributes.Add("href", "#");
                        //a.SetInnerText("Delete");

                        tdLast.InnerHtml += a;
                    }
                }

                tr.InnerHtml += tdLast;
                tag.InnerHtml += tr;
            }
        }

        private static void AppendLastRow(HtmlHelper html, TagBuilder tag, List<PropertyInfo> properties, string fieldName, Dictionary<string, IDictionary<string, object>> propertyAttributes, Dictionary<string, IDictionary<string, string>> formattingAttributes, int index)
        {
            var fName = fieldName + "[" + index + "].";

            var tr = new TagBuilder("tr");
            tr.Attributes.Add("class", "table-list-mvc-item-new");
            tr.Attributes.Add("data-item-index", index.ToString());

            foreach (var prop in properties)
            {
                var isHidden = Attribute.IsDefined(prop, typeof(TableListHiddenInput));
                if (isHidden)
                {
                    continue;
                }

                //if (propertyAttributes[prop.Name].ContainsKey("readonly"))
                //{
                //    propertyAttributes[prop.Name].Remove("readonly");
                //}

                //TODO FIX
                propertyAttributes[prop.Name]["class"] += " table-list-mvc-ignore";

                var td = new TagBuilder("td");

                var propType = GetPropertyType(prop);
                if (propType == typeof(bool))
                {
                    td.InnerHtml += html.CheckBox(fName + prop.Name, false, propertyAttributes[prop.Name]);
                }
                else
                {
                    td.InnerHtml += html.TextBox(fName + prop.Name, null, formattingAttributes[prop.Name]["DisplayFormatString"], propertyAttributes[prop.Name]);
                }
                
                td.InnerHtml += html.ValidationMessage(fName + prop.Name);

                tr.InnerHtml += td;
            }

            var tdLast = new TagBuilder("td");

            foreach (var prop in properties)
            {
                var isHidden = Attribute.IsDefined(prop, typeof(TableListHiddenInput));
                if (!isHidden)
                {
                    continue;
                }

                if (prop.Name == nameof(TableListItem.TL_State))
                {
                    tdLast.InnerHtml += html.Hidden(fName + prop.Name, TableListItemState.Added);
                }
                else if (prop.Name == nameof(TableListItem.TL_AllowModify))
                {
                    tdLast.InnerHtml += html.Hidden(fName + prop.Name, true);
                }
                else if (prop.Name == nameof(TableListItem.TL_AllowDelete))
                {
                    tdLast.InnerHtml += html.Hidden(fName + prop.Name, true);

                    var a = new TagBuilder("a");
                    a.Attributes.Add("class", "table-list-mvc-item-delete");
                    a.Attributes.Add("href", "#");
                    //a.SetInnerText("Delete");

                    tdLast.InnerHtml += a;
                }
                else
                {
                    tdLast.InnerHtml += html.Hidden(fName + prop.Name);
                }
            }

            tr.InnerHtml += tdLast;
            tag.InnerHtml += tr;
        }

        private static void AppendHead(TagBuilder tag, List<PropertyInfo> properties, Dictionary<string, IDictionary<string, string>> formattingAttributes)
        {
            var thead = new TagBuilder("thead");
            var tr = new TagBuilder("tr");

            foreach (var prop in properties)
            {
                var isHidden = Attribute.IsDefined(prop, typeof(TableListHiddenInput));
                if (isHidden)
                {
                    continue;
                }

                var th = new TagBuilder("th");
                th.SetInnerText(formattingAttributes[prop.Name]["DisplayName"] ?? prop.Name);
                tr.InnerHtml += th;
            }

            var thEmpty = new TagBuilder("th");
            tr.InnerHtml += thEmpty;

            thead.InnerHtml += tr;
            tag.InnerHtml += thead;
        }

        private static Dictionary<string, IDictionary<string, object>> CloneDictionary(Dictionary<string, IDictionary<string, object>> propertyAttributes)
        {
            Dictionary<string, IDictionary<string, object>> propertyAttributesClone = new Dictionary<string, IDictionary<string, object>>();
            foreach (var prop in propertyAttributes)
            {
                propertyAttributesClone.Add(prop.Key, new Dictionary<string, object>());
                foreach (var item in prop.Value)
                {
                    propertyAttributesClone[prop.Key].Add(new KeyValuePair<string, object>(item.Key, item.Value));
                }
            }

            return propertyAttributesClone;
        }

        private static Type GetPropertyType(PropertyInfo prop)
        {
            Type type;
            if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                type = prop.PropertyType.GetGenericArguments()[0];
            }
            else
            {
                type = prop.PropertyType;
            }

            return type;
        }
    }
}

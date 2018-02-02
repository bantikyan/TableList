using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;

namespace Zetalex.TableList.Mvc.Core
{
    public static class HtmlHelpers
    {
        public static IHtmlContent TableListFor<TModel, TProperty>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, bool showHeader = false, bool allowAdd = true)
        {
            return TableListFor(html, expression, null, showHeader: showHeader, allowAdd: allowAdd);
        }

        public static IHtmlContent TableListFor<TModel, TProperty>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, object htmlAttributes, bool showHeader = false, bool allowAdd = true)
        {
            return TableListFor(html, expression, HtmlHelper.AnonymousObjectToHtmlAttributes(htmlAttributes), showHeader: showHeader, allowAdd: allowAdd);
        }

        public static IHtmlContent TableListFor<TModel, TProperty>(this IHtmlHelper<TModel> html, Expression<Func<TModel, TProperty>> expression, IDictionary<string, object> htmlAttributes, bool showHeader = false, bool allowAdd = true)
        {
            var fieldName = ExpressionHelper.GetExpressionText(expression);
            //var fullBindingName = html.ViewContext.ViewData.TemplateInfo.GetFullHtmlFieldName(fieldName);
            //var fieldId = TagBuilder.CreateSanitizedId(fullBindingName);

            var metadata = ExpressionMetadataProvider.FromLambdaExpression(expression, html.ViewData, html.MetadataProvider);
            dynamic model = metadata.Model;
            Type baseType = metadata.ModelType.GetGenericArguments().Single();

            var properties = baseType.GetProperties().ToList();
            var typeMetadata = html.MetadataProvider.GetMetadataForType(baseType);

            Dictionary<string, IDictionary<string, object>> propertyAttributes = new Dictionary<string, IDictionary<string, object>>();
            Dictionary<string, Dictionary<string, string>> formattingAttributes = new Dictionary<string, Dictionary<string, string>>();

            foreach (var prop in properties)
            {
                var typeName = GetPropertyTypeName(prop);

                var propertyData = typeMetadata.Properties.Where(x => x.PropertyName == prop.Name).FirstOrDefault();
                //var attr = html.GetUnobtrusiveValidationAttributes(prop.Name, propertyData);
                var attr = new Dictionary<string, object>();
                //attr.Add("class", "form-control");
                attr.Add("class", "");
                attr.Add("placeholder", propertyData.DisplayName ?? prop.Name);
                if (propertyData.IsReadOnly)
                {
                    attr.Add("readonly", "readonly");
                }

                if (typeName == "DateTime")
                {
                    attr["class"] += " date-picker";
                }

                propertyAttributes.Add(prop.Name, attr);

                Dictionary<string, string> fAttr = new Dictionary<string, string>();
                fAttr.Add(nameof(propertyData.DisplayName), propertyData.DisplayName);
                fAttr.Add(nameof(propertyData.DisplayFormatString), propertyData.DisplayFormatString);
                formattingAttributes.Add(prop.Name, fAttr);
            }

            StringBuilder sb = new StringBuilder();
            if (showHeader)
            {
                BuildTableHeader(sb, properties, formattingAttributes);
            }

            sb.AppendLine("<tbody>");

            if (model != null && model.Count > 0)
            {
                BuildTableRows(html, sb, properties, model, fieldName, propertyAttributes, formattingAttributes);
            }

            if (allowAdd)
            {
                BuildLastTableRow(html, sb, properties, fieldName, propertyAttributes, formattingAttributes, model != null ? model.Count : 0);
            }

            sb.AppendLine("</tbody>");

            TagBuilder tag = new TagBuilder("table");
            if (htmlAttributes != null)
            {
                tag.MergeAttributes(htmlAttributes);
            }

            if (tag.Attributes.ContainsKey("class"))
            {
                tag.Attributes["class"] += " table-list-mvc";
            }
            else
            {
                tag.Attributes.Add("class", "table-list-mvc");
            }

            tag.InnerHtml.SetContent(sb.ToString());

            return new HtmlString(TagBuilderToString(tag));
        }

        private static void BuildTableRows(IHtmlHelper html, StringBuilder sb, List<PropertyInfo> properties, dynamic items, string fieldName, Dictionary<string, IDictionary<string, object>> propertyAttributes, Dictionary<string, Dictionary<string, string>> formattingAttributes)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var fName = fieldName + "[" + i + "].";

                sb.AppendLine("<tr class=\"table-list-mvc-item-view\" data-item-index=\"" + i + "\">");

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

                    sb.Append("<td>");
                    sb.Append(html.TextBox(fName + prop.Name, prop.GetValue((TableListItem)items[i]), formattingAttributes[prop.Name]["DisplayFormatString"], propertyAttributesClone[prop.Name]));
                    sb.Append(html.ValidationMessage(fName + prop.Name));
                    sb.AppendLine("</td>");
                }

                sb.Append("<td>");

                foreach (var prop in properties)
                {
                    var isHidden = Attribute.IsDefined(prop, typeof(TableListHiddenInput));
                    if (!isHidden)
                    {
                        continue;
                    }

                    var val = prop.GetValue((TableListItem)items[i]);
                    sb.Append(html.Hidden(fName + prop.Name, val));
                    if (prop.Name == nameof(TableListItem.TL_AllowDelete) && (bool)val)
                    {
                        sb.Append("<a href=\"#\" class=\"table-list-mvc-item-delete\">Delete</a>");
                    }
                }

                sb.AppendLine("</td>");

                sb.AppendLine("</tr>");
            }
        }

        private static void BuildLastTableRow(IHtmlHelper html, StringBuilder sb, List<PropertyInfo> properties, string fieldName, Dictionary<string, IDictionary<string, object>> propertyAttributes, Dictionary<string, Dictionary<string, string>> formattingAttributes, int index)
        {
            var fName = fieldName + "[" + index + "].";

            sb.AppendLine("<tr class=\"table-list-mvc-item-new\" data-item-index=\"" + index + "\">");

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

                sb.Append("<td>");
                sb.Append(html.TextBox(fName + prop.Name, null, formattingAttributes[prop.Name]["DisplayFormatString"], propertyAttributes[prop.Name]));
                //sb.Append(html.CheckBox(fName + prop.Name, false, propertyAttributes[prop.Name]));
                sb.Append(html.ValidationMessage(fName + prop.Name));
                sb.AppendLine("</td>");
            }

            sb.Append("<td>");

            foreach (var prop in properties)
            {
                var isHidden = Attribute.IsDefined(prop, typeof(TableListHiddenInput));
                if (!isHidden)
                {
                    continue;
                }

                if (prop.Name == nameof(TableListItem.TL_State))
                {
                    sb.Append(html.Hidden(fName + prop.Name, TableListItemState.Added));
                }
                else if (prop.Name == nameof(TableListItem.TL_AllowModify))
                {
                    sb.Append(html.Hidden(fName + prop.Name, true));
                }
                else if (prop.Name == nameof(TableListItem.TL_AllowDelete))
                {
                    sb.Append(html.Hidden(fName + prop.Name, true));
                    sb.Append("<a href=\"#\" class=\"table-list-mvc-item-delete\">Delete</a>");
                }
                else
                {
                    sb.Append(html.Hidden(fName + prop.Name));
                }
            }

            sb.AppendLine("</td>");
            sb.AppendLine("</tr>");
        }

        private static void BuildTableHeader(StringBuilder sb, List<PropertyInfo> properties, Dictionary<string, Dictionary<string, string>> formattingAttributes)
        {
            sb.AppendLine("<thead>");
            sb.AppendLine("<tr>");

            foreach (var prop in properties)
            {
                var isHidden = Attribute.IsDefined(prop, typeof(TableListHiddenInput));
                if (isHidden)
                {
                    continue;
                }

                sb.Append("<th>");
                sb.Append(formattingAttributes[prop.Name]["DisplayName"] ?? prop.Name);
                sb.AppendLine("</th>");
            }

            sb.AppendLine("<th></th>");

            sb.AppendLine("</tr>");
            sb.AppendLine("</thead>");
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

        private static string GetPropertyTypeName(PropertyInfo prop)
        {
            string typeName = "";
            if (prop.PropertyType.IsGenericType && prop.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                typeName = prop.PropertyType.GetGenericArguments()[0].Name;
            }
            else
            {
                typeName = prop.PropertyType.Name;
            }

            return typeName;
        }

        private static string TagBuilderToString(TagBuilder tagBuilder, TagRenderMode renderMode = TagRenderMode.Normal)
        {
            var encoder = HtmlEncoder.Create(new TextEncoderSettings());
            var writer = new StringWriter() as TextWriter;
            tagBuilder.WriteTo(writer, encoder);
            return writer.ToString();
        }
    }
}

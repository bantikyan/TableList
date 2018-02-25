using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.DataAnnotations;
using Microsoft.AspNetCore.Mvc.DataAnnotations.Internal;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.ViewFeatures.Internal;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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

            var propertyAttributes = new Dictionary<string, IDictionary<string, object>>();
            var formattingAttributes = new Dictionary<string, IDictionary<string, string>>();

            foreach (var prop in properties)
            {
                var propType = GetPropertyType(prop);

                var propertyData = typeMetadata.Properties.Where(x => x.PropertyName == prop.Name).FirstOrDefault();
                var attrs = GetUnobtrusiveValidationAttributes(prop, propertyData, html.MetadataProvider);

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

            table.InnerHtml.AppendHtml(tbody);

            return new HtmlString(TagBuilderToString(table));
        }

        private static void AppendRows(IHtmlHelper html, TagBuilder tag, List<PropertyInfo> properties, dynamic items, string fieldName, Dictionary<string, IDictionary<string, object>> propertyAttributes, Dictionary<string, IDictionary<string, string>> formattingAttributes)
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
                    
                    td.InnerHtml.AppendHtml(html.TextBox(fName + prop.Name, prop.GetValue((TableListItem)items[i]), formattingAttributes[prop.Name]["DisplayFormatString"], propertyAttributesClone[prop.Name]));
                    td.InnerHtml.AppendHtml(html.ValidationMessage(fName + prop.Name));

                    tr.InnerHtml.AppendHtml(td);
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
                    tdLast.InnerHtml.AppendHtml(html.Hidden(fName + prop.Name, val));

                    if (prop.Name == nameof(TableListItem.TL_AllowDelete) && (bool)val)
                    {
                        var a = new TagBuilder("a");
                        a.Attributes.Add("class", "table-list-mvc-item-delete");
                        a.Attributes.Add("href", "#");
                        a.InnerHtml.SetContent("Delete");

                        tdLast.InnerHtml.AppendHtml(a);
                    }
                }

                tr.InnerHtml.AppendHtml(tdLast);
                tag.InnerHtml.AppendHtml(tr);
            }
        }

        private static void AppendLastRow(IHtmlHelper html, TagBuilder tag, List<PropertyInfo> properties, string fieldName, Dictionary<string, IDictionary<string, object>> propertyAttributes, Dictionary<string, IDictionary<string, string>> formattingAttributes, int index)
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
                td.InnerHtml.AppendHtml(html.TextBox(fName + prop.Name, null, formattingAttributes[prop.Name]["DisplayFormatString"], propertyAttributes[prop.Name]));
                td.InnerHtml.AppendHtml(html.ValidationMessage(fName + prop.Name));

                tr.InnerHtml.AppendHtml(td);
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
                    tdLast.InnerHtml.AppendHtml(html.Hidden(fName + prop.Name, TableListItemState.Added));
                }
                else if (prop.Name == nameof(TableListItem.TL_AllowModify))
                {
                    tdLast.InnerHtml.AppendHtml(html.Hidden(fName + prop.Name, true));
                }
                else if (prop.Name == nameof(TableListItem.TL_AllowDelete))
                {
                    tdLast.InnerHtml.AppendHtml(html.Hidden(fName + prop.Name, true));

                    var a = new TagBuilder("a");
                    a.Attributes.Add("class", "table-list-mvc-item-delete");
                    a.Attributes.Add("href", "#");
                    a.InnerHtml.SetContent("Delete");

                    tdLast.InnerHtml.AppendHtml(a);
                }
                else
                {
                    tdLast.InnerHtml.AppendHtml(html.Hidden(fName + prop.Name));
                }
            }

            tr.InnerHtml.AppendHtml(tdLast);
            tag.InnerHtml.AppendHtml(tr);
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
                th.InnerHtml.SetContent(formattingAttributes[prop.Name]["DisplayName"] ?? prop.Name);
                tr.InnerHtml.AppendHtml(th);
            }

            var thEmpty = new TagBuilder("th");
            tr.InnerHtml.AppendHtml(thEmpty);

            thead.InnerHtml.AppendHtml(tr);
            tag.InnerHtml.AppendHtml(thead);
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

        private static string TagBuilderToString(TagBuilder tagBuilder, TagRenderMode renderMode = TagRenderMode.Normal)
        {
            var writer = new System.IO.StringWriter();
            tagBuilder.WriteTo(writer, HtmlEncoder.Default);
            return writer.ToString();
        }

        private static IDictionary<string, object> GetUnobtrusiveValidationAttributes(PropertyInfo propertyInfo, ModelMetadata modelMetadata, IModelMetadataProvider provider)
        {
            var hasRequiredAttribute = false;
            var propType = GetPropertyType(propertyInfo);
            var attrs = new AttributeDictionary();

            var valAttrs = propertyInfo.GetCustomAttributes(typeof(ValidationAttribute));

            ValidationAttributeAdapterProvider adapterProvider = new ValidationAttributeAdapterProvider();
            IAttributeAdapter adapter;

            var context = new ClientModelValidationContext(
                new ActionContext(),
                modelMetadata,
                provider,
                attrs);

            foreach (ValidationAttribute valAttr in valAttrs)
            {
                if (valAttr is RequiredAttribute)
                {
                    hasRequiredAttribute = true;
                }

                adapter = adapterProvider.GetAttributeAdapter(valAttr, null);
                adapter.AddValidation(context);
            }

            if (!hasRequiredAttribute && context.ModelMetadata.IsRequired)
            {
                adapter = adapterProvider.GetAttributeAdapter(new RequiredAttribute(), null);
                adapter.AddValidation(context);
            }

            if (propType == typeof(float) ||
                propType == typeof(double) ||
                propType == typeof(decimal))
            {
                (new NumericClientModelValidator()).AddValidation(context);
            }

            return attrs.ToDictionary(p => p.Key, pp => (object)pp.Value);
        }
    }
}

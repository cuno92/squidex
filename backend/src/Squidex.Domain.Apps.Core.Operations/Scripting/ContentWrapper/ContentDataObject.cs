// ==========================================================================
//  Squidex Headless CMS
// ==========================================================================
//  Copyright (c) Squidex UG (haftungsbeschränkt)
//  All rights reserved. Licensed under the MIT license.
// ==========================================================================

using System.Collections.Generic;
using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Squidex.Domain.Apps.Core.Contents;
using Squidex.Infrastructure;

#pragma warning disable RECS0133 // Parameter name differs in base declaration

namespace Squidex.Domain.Apps.Core.Scripting.ContentWrapper
{
    public sealed class ContentDataObject : ObjectInstance
    {
        private readonly NamedContentData contentData;
        private HashSet<JsValue> fieldsToDelete;
        private Dictionary<JsValue, PropertyDescriptor> fieldProperties;
        private bool isChanged;

        public ContentDataObject(Engine engine, NamedContentData contentData)
            : base(engine)
        {
            this.contentData = contentData;
        }

        public void MarkChanged()
        {
            isChanged = true;
        }

        public bool TryUpdate(out NamedContentData result)
        {
            result = contentData;

            if (isChanged)
            {
                if (fieldsToDelete != null)
                {
                    foreach (var field in fieldsToDelete)
                    {
                        contentData.Remove(field.AsString());
                    }
                }

                if (fieldProperties != null)
                {
                    foreach (var (key, propertyDescriptor) in fieldProperties)
                    {
                        var value = (ContentDataProperty)propertyDescriptor;

                        if (value.ContentField != null && value.ContentField.TryUpdate(out var fieldData))
                        {
                            contentData[key.AsString()] = fieldData;
                        }
                    }
                }
            }

            return isChanged;
        }

        public override void RemoveOwnProperty(JsValue property)
        {
            if (fieldsToDelete == null)
            {
                fieldsToDelete = new HashSet<JsValue>();
            }

            fieldsToDelete.Add(property);
            fieldProperties?.Remove(property);

            MarkChanged();
        }

        public override bool DefineOwnProperty(JsValue property, PropertyDescriptor desc)
        {
            EnsurePropertiesInitialized();

            if (!fieldProperties.ContainsKey(property))
            {
                fieldProperties[property] = new ContentDataProperty(this) { Value = desc.Value };
            }

            return true;
        }

        public override PropertyDescriptor GetOwnProperty(JsValue property)
        {
            EnsurePropertiesInitialized();

            return fieldProperties.GetOrAdd(property, this, (k, c) => new ContentDataProperty(c, new ContentFieldObject(c, new ContentFieldData(), false)));
        }

        public override IEnumerable<KeyValuePair<JsValue, PropertyDescriptor>> GetOwnProperties()
        {
            EnsurePropertiesInitialized();

            return fieldProperties;
        }

        private void EnsurePropertiesInitialized()
        {
            if (fieldProperties == null)
            {
                fieldProperties = new Dictionary<JsValue, PropertyDescriptor>(contentData.Count);

                foreach (var (key, value) in contentData)
                {
                    fieldProperties.Add(key, new ContentDataProperty(this, new ContentFieldObject(this, value, false)));
                }
            }
        }
    }
}

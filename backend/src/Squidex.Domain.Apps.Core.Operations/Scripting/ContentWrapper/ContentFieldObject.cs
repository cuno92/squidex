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
    public sealed class ContentFieldObject : ObjectInstance
    {
        private readonly ContentDataObject contentData;
        private readonly ContentFieldData? fieldData;
        private HashSet<JsValue> valuesToDelete;
        private Dictionary<JsValue, PropertyDescriptor> valueProperties;
        private bool isChanged;

        public ContentFieldData? FieldData
        {
            get { return fieldData; }
        }

        public ContentFieldObject(ContentDataObject contentData, ContentFieldData? fieldData, bool isNew)
            : base(contentData.Engine)
        {
            this.contentData = contentData;
            this.fieldData = fieldData;

            if (isNew)
            {
                MarkChanged();
            }
        }

        public void MarkChanged()
        {
            isChanged = true;

            contentData.MarkChanged();
        }

        public bool TryUpdate(out ContentFieldData? result)
        {
            result = fieldData;

            if (isChanged && fieldData != null)
            {
                if (valuesToDelete != null)
                {
                    foreach (var field in valuesToDelete)
                    {
                        fieldData.Remove(field.AsString());
                    }
                }

                if (valueProperties != null)
                {
                    foreach (var (key, propertyDescriptor) in valueProperties)
                    {
                        var value = (ContentFieldProperty)propertyDescriptor;

                        if (value.IsChanged)
                        {
                            fieldData[key.AsString()] = value.ContentValue;
                        }
                    }
                }
            }

            return isChanged;
        }

        public override void RemoveOwnProperty(JsValue property)
        {
            valuesToDelete ??= new HashSet<JsValue>();
            valuesToDelete.Add(property);

            valueProperties?.Remove(property);

            MarkChanged();
        }

        public override bool DefineOwnProperty(JsValue property, PropertyDescriptor desc)
        {
            EnsurePropertiesInitialized();

            if (!valueProperties.ContainsKey(property))
            {
                valueProperties[property] = new ContentFieldProperty(this) { Value = desc.Value };
            }

            return true;
        }

        public override PropertyDescriptor GetOwnProperty(JsValue property)
        {
            EnsurePropertiesInitialized();

            return valueProperties?.GetOrDefault(property) ?? PropertyDescriptor.Undefined;
        }

        public override IEnumerable<KeyValuePair<JsValue, PropertyDescriptor>> GetOwnProperties()
        {
            EnsurePropertiesInitialized();

            return valueProperties;
        }

        private void EnsurePropertiesInitialized()
        {
            if (valueProperties == null)
            {
                valueProperties = new Dictionary<JsValue, PropertyDescriptor>(fieldData?.Count ?? 0);

                if (fieldData != null)
                {
                    foreach (var (key, value) in fieldData)
                    {
                        valueProperties.Add(key, new ContentFieldProperty(this, value));
                    }
                }
            }
        }
    }
}

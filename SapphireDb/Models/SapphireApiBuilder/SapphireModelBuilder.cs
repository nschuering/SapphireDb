﻿using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using SapphireDb.Attributes;
using SapphireDb.Helper;

namespace SapphireDb.Models.SapphireApiBuilder
{
    public class SapphireModelBuilder<T>
    {
        private readonly ModelAttributesInfo attributesInfo;

        public SapphireModelBuilder()
        {
            attributesInfo = typeof(T).GetModelAttributesInfo();
        }

        public SapphireModelBuilder<T> SetQueryFunction(
            Func<HttpInformation, Expression<Func<T, bool>>> expressionBuilder)
        {
            if (attributesInfo.QueryFunctionAttribute == null)
            {
                attributesInfo.QueryFunctionAttribute = new QueryFunctionAttribute(null);
            }

            attributesInfo.QueryFunctionAttribute.FunctionBuilder = expressionBuilder;

            return this;
        }

        public SapphireModelBuilder<T> AddQueryAuth(string policies = null,
            Func<HttpInformation, T, bool> function = null)
        {
            attributesInfo.QueryAuthAttributes.Add(CreateAuthAttribute<QueryAuthAttribute>(policies, function));
            return this;
        }

        public SapphireModelBuilder<T> AddQueryEntryAuth(string policies = null,
            Func<HttpInformation, T, bool> function = null)
        {
            attributesInfo.QueryEntryAuthAttributes.Add(CreateAuthAttribute<QueryEntryAuthAttribute>(policies, function));
            return this;
        }

        public SapphireModelBuilder<T> AddCreateAuth(string policies = null,
            Func<HttpInformation, T, bool> function = null)
        {
            attributesInfo.CreateAuthAttributes.Add(CreateAuthAttribute<CreateAuthAttribute>(policies, function));
            return this;
        }

        public SapphireModelBuilder<T> AddUpdateAuth(string policies = null,
            Func<HttpInformation, T, bool> function = null)
        {
            attributesInfo.UpdateAuthAttributes.Add(CreateAuthAttribute<UpdateAuthAttribute>(policies, function));
            return this;
        }

        public SapphireModelBuilder<T> AddRemoveAuth(string policies = null,
            Func<HttpInformation, T, bool> function = null)
        {
            attributesInfo.RemoveAuthAttributes.Add(CreateAuthAttribute<RemoveAuthAttribute>(policies, function));
            return this;
        }

        private TAttributeType CreateAuthAttribute<TAttributeType>(string policies,
            Func<HttpInformation, T, bool> function) where TAttributeType : AuthAttributeBase
        {
            TAttributeType attribute = (TAttributeType) Activator.CreateInstance(typeof(TAttributeType), policies, null);
            
            if (function != null)
            {
                attribute.FunctionLambda = (information, model) => function(information, (T) model);
            }

            return attribute;
        }

        public SapphireModelBuilder<T> AddCreateEvent(Action<T, HttpInformation> before = null,
            Action<T, HttpInformation> beforeSave = null, Action<T, HttpInformation> after = null)
        {
            attributesInfo.CreateEventAttributes.Add(
                CreateEventAttribute<CreateEventAttribute>(before, beforeSave, after));
            return this;
        }

        public SapphireModelBuilder<T> AddUpdateEvent(Action<T, HttpInformation> before = null,
            Action<T, HttpInformation> beforeSave = null, Action<T, HttpInformation> after = null)
        {
            attributesInfo.UpdateEventAttributes.Add(
                CreateEventAttribute<UpdateEventAttribute>(before, beforeSave, after));
            return this;
        }

        public SapphireModelBuilder<T> AddRemoveEvent(Action<T, HttpInformation> before = null,
            Action<T, HttpInformation> beforeSave = null, Action<T, HttpInformation> after = null)
        {
            attributesInfo.RemoveEventAttributes.Add(
                CreateEventAttribute<RemoveEventAttribute>(before, beforeSave, after));
            return this;
        }

        private TAttributeType CreateEventAttribute<TAttributeType>(
            Action<T, HttpInformation> before, Action<T, HttpInformation> beforeSave,
            Action<T, HttpInformation> after)
            where TAttributeType : ModelStoreEventAttributeBase
        {
            TAttributeType attribute = (TAttributeType)Activator.CreateInstance(typeof(TAttributeType), null, null, null);

            if (before != null)
            {
                attribute.BeforeLambda = (model, information) => before((T) model, information);
            }

            if (beforeSave != null)
            {
                attribute.BeforeSaveLambda = (model, information) => beforeSave((T) model, information);
            }

            if (after != null)
            {
                attribute.AfterLambda = (model, information) => after((T) model, information);
            }

            return attribute;
        }

        public SapphirePropertyBuilder<T, TProperty> Property<TProperty>(Expression<Func<T, TProperty>> selector)
        {
            PropertyInfo property = (PropertyInfo)((MemberExpression) selector.Body).Member;
            return new SapphirePropertyBuilder<T, TProperty>(property);
        }
    }
}
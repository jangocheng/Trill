﻿// *********************************************************************
// Copyright (c) Microsoft Corporation.  All rights reserved.
// Licensed under the MIT License
// *********************************************************************
using System;
using System.Diagnostics.Contracts;
using System.Reflection;
using Microsoft.StreamProcessing.Internal;

namespace Microsoft.StreamProcessing
{
    internal partial class EndEdgeFreeOutputTemplate
    {
        private static int EndEdgeFreeOutputTemplateSequenceNumber = 0;
        private Func<string, string, string> keyEquals;
        private Func<string, string> keyHashFunction;
        private Func<string, string, string> payloadEquals;
        private Func<string, string> payloadHashFunction;
        private string ActiveEventType;

        private EndEdgeFreeOutputTemplate(string className, Type keyType, Type payloadType)
            : base(className, keyType, payloadType, payloadType) { }

        /// <summary>
        /// Generate a batch class definition to be used as a EndEdgeFreeOutputStreamable pipe.
        /// Compile the definition, dynamically load the assembly containing it, and return the Type representing the
        /// union pipe class.
        /// </summary>
        /// <typeparam name="TKey">The key type for both sides.</typeparam>
        /// <typeparam name="TPayload">The payload type.</typeparam>
        /// <returns>
        /// A type that is defined to be a subtype of UnaryPipe&lt;<typeparamref name="TKey"/>,<typeparamref name="TPayload"/>, <typeparamref name="TPayload"/>&gt;.
        /// </returns>
        internal static Tuple<Type, string> Generate<TKey, TPayload>(
            EndEdgeFreeOutputStreamable<TKey, TPayload> stream)
        {
            Contract.Requires(stream != null);
            Contract.Ensures(Contract.Result<Tuple<Type, string>>() == null || typeof(UnaryPipe<TKey, TPayload, TPayload>).GetTypeInfo().IsAssignableFrom(Contract.Result<Tuple<Type, string>>().Item1));

            var template = new EndEdgeFreeOutputTemplate(
                string.Format("GeneratedEndEdgeFreeOutput_{0}", EndEdgeFreeOutputTemplateSequenceNumber++),
                typeof(TKey), typeof(TPayload));

            template.ActiveEventType = typeof(TPayload).GetTypeInfo().IsValueType ? template.TPayload : "Active_Event";

            #region Key Equals
            var keyComparer = stream.Properties.KeyEqualityComparer.GetEqualsExpr();
            template.keyEquals =
                (left, right) =>
                    keyComparer.Inline(left, right);
            #endregion

            #region Key Hash Function
            var keyHashFunction = stream.Properties.KeyEqualityComparer.GetGetHashCodeExpr();
            template.keyHashFunction = (e => keyHashFunction.Inline(e));
            #endregion

            #region Payload Equals
            var payloadComparer = stream.Properties.PayloadEqualityComparer.GetEqualsExpr();
            template.payloadEquals = (left, right) => payloadComparer.Inline(left, right);
            #endregion

            #region Payload Hash Function
            var payloadHashFunction = stream.Properties.PayloadEqualityComparer.GetGetHashCodeExpr();
            template.payloadHashFunction = (aa) => payloadHashFunction.Inline(aa);
            #endregion

            return template.Generate<TKey, TPayload>(typeof(IStreamable<,>), typeof(FastDictionaryGenerator2));
        }
    }
}

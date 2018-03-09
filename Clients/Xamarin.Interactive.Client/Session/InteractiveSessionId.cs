//
// Author:
//   Aaron Bockover <abock@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace Xamarin.Interactive.Session
{
    public struct InteractiveSessionId : IEquatable<InteractiveSessionId>
    {
        readonly string id;

        public InteractiveSessionId (string id)
            => this.id = id ?? throw new ArgumentNullException (nameof (id));

        public bool Equals (InteractiveSessionId other)
            => other.id == id;

        public override bool Equals (object obj)
            => obj is InteractiveSessionId sessionId && Equals (sessionId);

        public override int GetHashCode()
            => id.GetHashCode ();

        public override string ToString ()
            => id;

        public static implicit operator string (InteractiveSessionId sessionId)
            => sessionId.id;

        public static implicit operator InteractiveSessionId (string id)
            => new InteractiveSessionId (id);

        public static implicit operator InteractiveSessionId (Guid id)
            => new InteractiveSessionId (id == default ? null : id.ToString ());

        public static bool operator == (InteractiveSessionId a, InteractiveSessionId b)
            => a.Equals (b);

        public static bool operator != (InteractiveSessionId a, InteractiveSessionId b)
            => !(a.Equals (b));
    }
}
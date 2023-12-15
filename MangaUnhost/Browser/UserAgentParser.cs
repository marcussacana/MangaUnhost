using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MangaUnhost.Browser
{
    public class UserAgent : IEquatable<UserAgent>
    {
        public UserAgent()
        {
            System = new HashSet<string>();
            Details = new HashSet<UserAgentPart>();
            Extensions = new HashSet<UserAgentPart>();
        }

        // from here https://developer.mozilla.org/en-US/docs/Web/HTTP/Headers/User-Agent
        public virtual UserAgentPart Product { get; set; }
        public virtual ISet<string> System { get; }
        public virtual UserAgentPart Platform { get; set; }
        public virtual ISet<UserAgentPart> Details { get; }
        public virtual ISet<UserAgentPart> Extensions { get; }

        public override int GetHashCode() => base.GetHashCode();
        public override bool Equals(object obj) => Equals(obj as UserAgentPart);
        public bool Equals(UserAgent other) => !ReferenceEquals(other, null) &&
            Equals(Product, other.Product) &&
            Equals(System, other.System) &&
            Equals(Platform, other.Platform) &&
            Equals(Details, other.Details) &&
            Equals(Extensions, other.Extensions);

        public static bool operator !=(UserAgent lhs, UserAgent rhs) => !(lhs == rhs);
        public static bool operator ==(UserAgent lhs, UserAgent rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                    return true;

                return false;
            }
            return lhs.Equals(rhs);
        }

        public override string ToString()
        {
            var list = new List<string>();
            if (Product != null)
            {
                list.Add(Product.ToString());
            }

            if (System.Count > 0)
            {
                list.Add("(" + string.Join("; ", System) + ")");
            }

            if (Platform != null)
            {
                list.Add(Platform.ToString());
            }

            if (Details.Count > 0)
            {
                list.Add("(" + string.Join("; ", Details) + ")");
            }

            if (Extensions.Count > 0)
            {
                list.Add(string.Join(" ", Extensions));
            }
            return string.Join(" ", list);
        }

        private static bool Equals(ISet<UserAgentPart> parts1, ISet<UserAgentPart> parts2)
        {
            if (parts1.Count != parts2.Count)
                return false;

            foreach (var kv in parts1)
            {
                if (!parts2.Contains(kv))
                    return false;
            }
            return true;
        }

        internal static string Nullify(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            text = text.Trim();
            return text.Length == 0 ? null : text;
        }

        public static UserAgent Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            UserAgentPart part;
            var space = text.IndexOf(' ');
            if (space < 0)
            {
                part = UserAgentPart.Parse(text);
                if (part == null)
                    return null;

                return new UserAgent { Product = part };
            }

            var product = text.Substring(0, space);
            part = UserAgentPart.Parse(product);
            if (part == null)
                return null;

            var ua = new UserAgent { Product = part };
            var offset = space;

            var startParen = text.IndexOf('(', space + 1);
            if (startParen >= 0)
            {
                var endParen = text.IndexOf(')', startParen + 1);
                if (endParen < 0) // syntax error
                    return ua;

                var system = Nullify(text.Substring(startParen + 1, endParen - startParen - 1));
                if (system != null)
                {
                    foreach (var sys in system.Split(';'))
                    {
                        var syst = Nullify(sys);
                        if (syst != null)
                        {
                            ua.System.Add(syst);
                        }
                    }
                }
                offset = endParen;
            }

            var platform = text.IndexOf(' ', offset + 1);
            if (platform < 0)
            {
                ua.Platform = UserAgentPart.Parse(Nullify(text.Substring(offset + 1)));
                return ua;
            }

            startParen = text.IndexOf('(', platform + 1);
            if (startParen >= 0)
            {
                ua.Platform = UserAgentPart.Parse(Nullify(text.Substring(platform + 1)));
                var endParen = text.IndexOf(')', startParen + 1);
                if (endParen < 0) // syntax error
                    return ua;

                var details = Nullify(text.Substring(startParen + 1, endParen - startParen - 1));
                if (details != null)
                {
                    foreach (var det in details.Split(';'))
                    {
                        var dett = UserAgentPart.Parse(Nullify(det));
                        if (dett != null)
                        {
                            ua.Details.Add(dett);
                        }
                    }
                }
                offset = endParen;
            }
            else
            {
                space = text.IndexOf(' ', platform + 1);
                if (space < 0)
                {
                    ua.Platform = UserAgentPart.Parse(Nullify(text.Substring(platform + 1)));
                    return ua;
                }

                ua.Platform = UserAgentPart.Parse(Nullify(text.Substring(platform + 1, space - platform - 1)));
                offset = space;
            }

            foreach (var ext in text.Substring(offset + 1).Split(' '))
            {
                var extt = UserAgentPart.Parse(Nullify(ext));
                if (extt != null)
                {
                    ua.Extensions.Add(extt);
                }
            }
            return ua;
        }
    }

    public class UserAgentPart : IEquatable<UserAgentPart>
    {
        public UserAgentPart(string name, string version = null)
        {
            if (name == null)
                throw new ArgumentNullException(nameof(name));

            Name = name;
            Version = version;
        }

        public string Name { get; }
        public string Version { get; }

        public override int GetHashCode()
        {
            var code = Name.GetHashCode();
            if (Version != null)
            {
                code ^= Version.GetHashCode();
            }
            return code;
        }

        public override string ToString() => Version != null ? Name + "/" + Version : Name;
        public override bool Equals(object obj) => Equals(obj as UserAgentPart);
        public bool Equals(UserAgentPart other) => !ReferenceEquals(other, null) && Name == other.Name && Version == other.Version;
        public static bool operator !=(UserAgentPart lhs, UserAgentPart rhs) => !(lhs == rhs);
        public static bool operator ==(UserAgentPart lhs, UserAgentPart rhs)
        {
            if (lhs is null)
            {
                if (rhs is null)
                    return true;

                return false;
            }
            return lhs.Equals(rhs);
        }

        public static UserAgentPart Parse(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            var space = text.IndexOf('/');
            if (space < 0)
                return new UserAgentPart(UserAgent.Nullify(text));

            var name = UserAgent.Nullify(text.Substring(0, space));
            if (name == null)
                return null;

            var version = UserAgent.Nullify(text.Substring(space + 1));
            var i = 0;
            for (; i < version.Length; i++)
            {
                var c = version[i];
                if (c != '.' && !char.IsDigit(c))
                    break;
            }

            if (i < (version.Length - 1))
            {
                version = version.Substring(0, i);
            }

            return new UserAgentPart(name, UserAgent.Nullify(version));
        }
    }
}

﻿//------------------------------------------------------------------------------
// <auto-generated>
//     O código foi gerado por uma ferramenta.
//     Versão de Tempo de Execução:4.0.30319.42000
//
//     As alterações ao arquivo poderão causar comportamento incorreto e serão perdidas se
//     o código for gerado novamente.
// </auto-generated>
//------------------------------------------------------------------------------

namespace MangaUnhost.Properties {
    using System;
    
    
    /// <summary>
    ///   Uma classe de recurso de tipo de alta segurança, para pesquisar cadeias de caracteres localizadas etc.
    /// </summary>
    // Essa classe foi gerada automaticamente pela classe StronglyTypedResourceBuilder
    // através de uma ferramenta como ResGen ou Visual Studio.
    // Para adicionar ou remover um associado, edite o arquivo .ResX e execute ResGen novamente
    // com a opção /str, ou recrie o projeto do VS.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Retorna a instância de ResourceManager armazenada em cache usada por essa classe.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("MangaUnhost.Properties.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Substitui a propriedade CurrentUICulture do thread atual para todas as
        ///   pesquisas de recursos que usam essa classe de recurso de tipo de alta segurança.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Consulta um recurso localizado do tipo System.Drawing.Bitmap.
        /// </summary>
        internal static System.Drawing.Bitmap Book {
            get {
                object obj = ResourceManager.GetObject("Book", resourceCulture);
                return ((System.Drawing.Bitmap)(obj));
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a document.getElementById(&quot;cf-please-wait&quot;).style.display == &quot;none&quot;.
        /// </summary>
        internal static string cloudflareCaptchaReady {
            get {
                return ResourceManager.GetString("cloudflareCaptchaReady", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a document.getElementById(&apos;recaptcha_submit&apos;).click();.
        /// </summary>
        internal static string CloudFlareSubmitCaptcha {
            get {
                return ResourceManager.GetString("CloudFlareSubmitCaptcha", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a &lt;DOCTYPE HTML&gt;
        ///&lt;html&gt;
        ///   &lt;head&gt;
        ///      &lt;meta charset=&quot;utf-8&quot;&gt;
        ///      &lt;title&gt;{0} - HTML Reader&lt;/title&gt;
        ///      &lt;style&gt;body{{background-color: #000000;}}&lt;/style&gt;
        ///   &lt;/head&gt;
        ///   &lt;body&gt;
        ///      &lt;div align=&quot;center&quot;&gt;
        ///{1}
        ///      &lt;/div&gt;
        ///   &lt;/body&gt;
        ///&lt;/html&gt;.
        /// </summary>
        internal static string ComicReaderHtmlBase {
            get {
                return ResourceManager.GetString("ComicReaderHtmlBase", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a &lt;DOCTYPE HTML&gt;
        ///&lt;html&gt;
        ///   &lt;meta charset=&quot;utf-8&quot;&gt;
        ///   &lt;head&gt;
        ///      &lt;title&gt;{0}&lt;/title&gt;
        ///      &lt;style&gt;body{{background-color: #000000;}}&lt;/style&gt;
        ///   &lt;/head&gt;
        ///   &lt;body&gt;
        ///      &lt;div align=&quot;center&quot;&gt;
        ///         &lt;img src=&quot;{1}&quot; style=&quot;max-width:100%;&quot;/&gt;&lt;br/&gt;
        ///.
        /// </summary>
        internal static string ComicReaderIndexBase {
            get {
                return ResourceManager.GetString("ComicReaderIndexBase", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a          &lt;a href=&quot;{0}&quot; style=&quot;color: #FFF;&quot;&gt;{1}&lt;/a&gt;&lt;/br&gt;.
        /// </summary>
        internal static string ComicReaderIndexChapterBase {
            get {
                return ResourceManager.GetString("ComicReaderIndexChapterBase", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a          &lt;img src=&quot;{0}&quot; style=&quot;max-width:100%;&quot; id=&quot;img{1}&quot;/&gt;&lt;br/&gt;.
        /// </summary>
        internal static string ComicReaderLastPageBase {
            get {
                return ResourceManager.GetString("ComicReaderLastPageBase", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a          &lt;a href=&quot;{0}&quot; style=&quot;color: #FFF;&quot;&gt;{1}&lt;/a&gt;.
        /// </summary>
        internal static string ComicReaderNextChapterBase {
            get {
                return ResourceManager.GetString("ComicReaderNextChapterBase", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a          &lt;img src=&quot;{0}&quot; style=&quot;max-width:100%;&quot; id=&quot;img{1}&quot; onload=&quot;img{2}.src = &apos;{3}&apos;&quot; onerror=&quot;img{2}.src = &apos;{3}&apos;&quot;/&gt;&lt;br/&gt;
        ///.
        /// </summary>
        internal static string ComicReaderPageBase {
            get {
                return ResourceManager.GetString("ComicReaderPageBase", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a document.readyState;.
        /// </summary>
        internal static string GetDocumentStatus {
            get {
                return ResourceManager.GetString("GetDocumentStatus", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a navigator.userAgent;.
        /// </summary>
        internal static string GetUserAgent {
            get {
                return ResourceManager.GetString("GetUserAgent", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a var hcFrame = function() { var frames = document.getElementsByTagName(&quot;iframe&quot;); for (var i = 0; i &lt; frames.length; i++) if (frames[i].src.indexOf(&quot;hcaptcha-challenge&quot;) &gt;= 0) return frames[i]; }
        ///var bounds = hcFrame().getBoundingClientRect();
        ///JSON.stringify({ x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height });.
        /// </summary>
        internal static string hCaptchaGetChallengeFramePosition {
            get {
                return ResourceManager.GetString("hCaptchaGetChallengeFramePosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a var hcFrame = function() { var frames = document.getElementsByTagName(&quot;iframe&quot;); for (var i = 0; i &lt; frames.length; i++) if (frames[i].src.indexOf(&quot;hcaptcha-checkbox&quot;) &gt;= 0) return frames[i]; }
        ///var bounds = hcFrame().getBoundingClientRect();
        ///JSON.stringify({ x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height });.
        /// </summary>
        internal static string hCaptchaGetMainFramePosition {
            get {
                return ResourceManager.GetString("hCaptchaGetMainFramePosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a var bounds = document.getElementsByClassName(&quot;button-submit&quot;)[0].getBoundingClientRect()
        ///JSON.stringify({ x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height });.
        /// </summary>
        internal static string hCaptchaGetVerifyButtonPosition {
            get {
                return ResourceManager.GetString("hCaptchaGetVerifyButtonPosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a document.getElementsByClassName(&quot;display-error&quot;)[0].style.opacity != &quot;0&quot;.
        /// </summary>
        internal static string hCaptchaIsFailed {
            get {
                return ResourceManager.GetString("hCaptchaIsFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a typeof(hcaptcha) == &quot;undefined&quot; || hcaptcha.getResponse() != &quot;&quot;.
        /// </summary>
        internal static string hCaptchaIsSolved {
            get {
                return ResourceManager.GetString("hCaptchaIsSolved", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a hcaptcha.reset();.
        /// </summary>
        internal static string hCaptchaReset {
            get {
                return ResourceManager.GetString("hCaptchaReset", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a var bounds = anchor.getBoundingClientRect();
        ///JSON.stringify({ x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height });.
        /// </summary>
        internal static string reCaptchaGetAnchorPosition {
            get {
                return ResourceManager.GetString("reCaptchaGetAnchorPosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a document.getElementsByClassName(&quot;rc-audiochallenge-tdownload-link&quot;)[0].href;.
        /// </summary>
        internal static string reCaptchaGetAudioUrl {
            get {
                return ResourceManager.GetString("reCaptchaGetAudioUrl", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a var bounds = bframe.getBoundingClientRect();
        ///JSON.stringify({ x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height });.
        /// </summary>
        internal static string reCaptchaGetBFramePosition {
            get {
                return ResourceManager.GetString("reCaptchaGetBFramePosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a grecaptcha.getResponse();.
        /// </summary>
        internal static string reCaptchaGetResponse {
            get {
                return ResourceManager.GetString("reCaptchaGetResponse", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a var AudioResp = document.getElementById(&apos;audio-response&apos;);
        ///var bounds = AudioResp.getBoundingClientRect();
        ///JSON.stringify({ x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height });.
        /// </summary>
        internal static string reCaptchaGetSoundResponsePosition {
            get {
                return ResourceManager.GetString("reCaptchaGetSoundResponsePosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a var AudioBnt = document.getElementById(&quot;recaptcha-audio-button&quot;);
        ///var bounds = AudioBnt.getBoundingClientRect();
        ///JSON.stringify({ x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height });.
        /// </summary>
        internal static string reCaptchaGetSpeakPosition {
            get {
                return ResourceManager.GetString("reCaptchaGetSpeakPosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a var VerifyBnt = document.getElementById(&apos;recaptcha-verify-button&apos;);
        ///var bounds = VerifyBnt.getBoundingClientRect();
        ///JSON.stringify({ x: bounds.x, y: bounds.y, width: bounds.width, height: bounds.height });.
        /// </summary>
        internal static string reCaptchaGetVerifyButtonPosition {
            get {
                return ResourceManager.GetString("reCaptchaGetVerifyButtonPosition", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a var iframes = document.getElementsByTagName(&quot;iframe&quot;);
        ///var anchor = null;
        ///var bframe = null;
        ///for (var i = 0; i &lt; iframes.length; i++){
        ///	var frame = iframes[i];
        ///	if (frame.src.indexOf(&quot;/anchor?&quot;) &gt; 0)
        ///		anchor = frame;
        ///	if (frame.src.indexOf(&quot;/bframe?&quot;) &gt; 0)
        ///		bframe = frame;
        ///}
        ///anchor.scrollIntoView();.
        /// </summary>
        internal static string reCaptchaIframeSearch {
            get {
                return ResourceManager.GetString("reCaptchaIframeSearch", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a document.getElementsByClassName(&apos;rc-doscaptcha-body-text&apos;).length != 0.
        /// </summary>
        internal static string reCaptchaIsFailed {
            get {
                return ResourceManager.GetString("reCaptchaIsFailed", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a grecaptcha.reset();.
        /// </summary>
        internal static string reCaptchaReset {
            get {
                return ResourceManager.GetString("reCaptchaReset", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Consulta uma cadeia de caracteres localizada semelhante a [{{000214A0-0000-0000-C000-000000000046}}]
        ///Prop3=19,2
        ///[InternetShortcut]
        ///IDList=
        ///URL={0}.
        /// </summary>
        internal static string UrlFile {
            get {
                return ResourceManager.GetString("UrlFile", resourceCulture);
            }
        }
    }
}

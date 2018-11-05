using System.Xml;
using System.Xml.Serialization;

namespace SD226856.PowerBIHttpModule
{
    [XmlRoot(ElementName = "SecurityHeader", Namespace = "http://AutodeskDM/Services")]
    public class SecurityHeader
    {
        [XmlElement(ElementName = "Ticket")]
        public string Ticket { get; set; }
        [XmlElement(ElementName = "UserId")]
        public string UserId { get; set; }
    }

    [XmlRoot(ElementName = "Header", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class Header
    {
        [XmlElement(ElementName = "SecurityHeader", Namespace = "http://AutodeskDM/Services")]
        public SecurityHeader SecurityHeader { get; set; }
    }

    [XmlRoot(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class Body
    {
        [XmlElement]
        public XmlElement XmlElement { get; set; }
    }

    [XmlRoot(ElementName = "Envelope", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class Envelope
    {
        [XmlElement(ElementName = "Header", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public Header Header { get; set; }

        [XmlElement(ElementName = "Body", Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
        public Body Body { get; set; }
    }
}

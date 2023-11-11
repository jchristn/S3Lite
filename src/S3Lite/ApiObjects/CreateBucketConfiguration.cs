namespace S3Lite.ApiObjects
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Xml.Serialization;

    /// <summary>
    /// Create bucket configuration.
    /// </summary>
    [XmlRoot(ElementName = "CreateBucketConfiguration", IsNullable = true)]
    public class CreateBucketConfiguration
    {
        // Namespace = "http://s3.amazonaws.com/doc/2006-03-01/"

        #region Public-Members

        /// <summary>
        /// Location constraint.
        /// </summary>
        [XmlElement(ElementName = "LocationConstraint", IsNullable = true)]
        public string LocationConstraint { get; set; } = "us-west-1";

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public CreateBucketConfiguration()
        {

        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}

namespace S3Lite.ApiObjects
{
    using System;
    using System.Xml.Serialization;

    /// <summary>
    /// Identifies a single object to delete as part of a delete-multiple (batch delete) request.
    /// </summary>
    [XmlRoot(ElementName = "Object", IsNullable = true)]
    public class ObjectIdentifier
    {
        // Namespace = "http://s3.amazonaws.com/doc/2006-03-01/"

        #region Public-Members

        /// <summary>
        /// Object key.  Required.
        /// </summary>
        [XmlElement(ElementName = "Key", IsNullable = true)]
        public string Key { get; set; } = null;

        /// <summary>
        /// Version ID of the object to delete.  Optional.
        /// When null or empty, the current version is targeted.
        /// </summary>
        [XmlElement(ElementName = "VersionId", IsNullable = true)]
        public string VersionId { get; set; } = null;

        #endregion

        #region Private-Members

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public ObjectIdentifier()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="key">Object key.</param>
        /// <param name="versionId">Version ID, or null to target the current version.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="key"/> is null or empty.</exception>
        public ObjectIdentifier(string key, string versionId = null)
        {
            if (String.IsNullOrEmpty(key)) throw new ArgumentNullException(nameof(key));
            Key = key;
            VersionId = versionId;
        }

        #endregion

        #region Public-Methods

        /// <summary>
        /// Helper method for XML serialization.
        /// </summary>
        /// <returns>Boolean.</returns>
        public bool ShouldSerializeVersionId()
        {
            return !String.IsNullOrEmpty(VersionId);
        }

        #endregion

        #region Private-Methods

        #endregion
    }
}

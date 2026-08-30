namespace S3Lite.ApiObjects
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// Request payload for a delete-multiple (batch delete) operation.
    /// Serializes to the S3 <c>Delete</c> document containing the objects to remove.
    /// </summary>
    [XmlRoot(ElementName = "Delete", IsNullable = true)]
    public class DeleteMultipleRequest
    {
        // Namespace = "http://s3.amazonaws.com/doc/2006-03-01/"

        #region Public-Members

        /// <summary>
        /// Enables quiet mode.  When true, the server returns only entries for objects that
        /// could not be deleted; successfully deleted objects are omitted from the response.
        /// Default value is <c>false</c>.
        /// </summary>
        [XmlElement(ElementName = "Quiet")]
        public bool Quiet { get; set; } = false;

        /// <summary>
        /// Objects to delete.  Must contain between 1 and 1000 entries.
        /// </summary>
        [XmlElement(ElementName = "Object", IsNullable = true)]
        public List<ObjectIdentifier> Objects
        {
            get
            {
                return _Objects;
            }
            set
            {
                _Objects = value ?? new List<ObjectIdentifier>();
            }
        }

        #endregion

        #region Private-Members

        private List<ObjectIdentifier> _Objects = new List<ObjectIdentifier>();

        #endregion

        #region Constructors-and-Factories

        /// <summary>
        /// Instantiate.
        /// </summary>
        public DeleteMultipleRequest()
        {

        }

        /// <summary>
        /// Instantiate.
        /// </summary>
        /// <param name="objects">Objects to delete.</param>
        /// <param name="quiet">Enable quiet mode.</param>
        public DeleteMultipleRequest(List<ObjectIdentifier> objects, bool quiet = false)
        {
            if (objects != null) Objects = objects;
            Quiet = quiet;
        }

        #endregion

        #region Public-Methods

        #endregion

        #region Private-Methods

        #endregion
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace XmlValidatorAndEditor
{
    public partial class MainForm : Form
    {
        #region Fields & Properties

        /// <summary>Stores the file path for the XSD header schema.</summary>
        private string xsdHeaderPath = string.Empty;

        /// <summary>Stores the file path for the XSD body schema.</summary>
        private string xsdBodyPath = string.Empty;

        /// <summary>Stores the file path for the currently loaded XML file.</summary>
        private string xmlPath = string.Empty;

        /// <summary>Collects error messages during XML validation.</summary>
        private List<string> validationErrors = new List<string>();

        /// <summary>Holds all dynamically generated UI row objects.</summary>
        private List<UiRow> allUiRows = new List<UiRow>();

        #endregion

        #region Constructor

        /// <summary>Initializes the main form components.</summary>
        public MainForm()
        {
            InitializeComponent();
        }

        #endregion

        #region Menu & Button Event Handlers

        /// <summary>Handles the 'Open XSD Header' menu item click to select an XSD header file.</summary>
        private void openXSDToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "XSD Files (*.xsd)|*.xsd|All files (*.*)|*.*";
                openFileDialog.Title = "Select XSD Header File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    xsdHeaderPath = openFileDialog.FileName;
                    lblStatus.Text = $"XSD Header loaded: {System.IO.Path.GetFileName(xsdHeaderPath)}";
                }
            }
        }

        /// <summary>Handles the 'Load XSD Body' menu item click to build the UI from a schema.</summary>
        private void loadXSDBodyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "XSD Files (*.xsd)|*.xsd|All files (*.*)|*.*";
                openFileDialog.Title = "Select XSD Body File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    xsdBodyPath = openFileDialog.FileName;
                    lblStatus.Text = $"XSD Body loaded: {System.IO.Path.GetFileName(xsdBodyPath)}";
                    PopulateLayoutFromSchema(xsdBodyPath);
                }
            }
        }

        /// <summary>Handles the 'Load XML' menu item click to populate the UI from an XML file.</summary>
        private void loadXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!allUiRows.Any())
            {
                MessageBox.Show("Please load an XSD schema before loading an XML file.", "Schema Required", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "XML Files (*.xml)|*.xml|All files (*.*)|*.*";
                openFileDialog.Title = "Select XML File";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    xmlPath = openFileDialog.FileName;
                    lblStatus.Text = $"XML file loaded: {System.IO.Path.GetFileName(xmlPath)}";
                    PopulateEditorsFromXml(xmlPath);
                }
            }
        }

        /// <summary>Handles the 'Save XML' menu item click, saving to the current path or triggering 'Save As'.</summary>
        private void saveXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(xmlPath))
            {
                SaveXmlToFile(xmlPath);
            }
            else
            {
                saveXMLAsToolStripMenuItem_Click(sender, e);
            }
        }

        /// <summary>Handles the 'Save XML As' menu item click to save the data to a new XML file.</summary>
        private void saveXMLAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "XML Files (*.xml)|*.xml|All files (*.*)|*.*";
                saveFileDialog.Title = "Save an XML File";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    SaveXmlToFile(saveFileDialog.FileName);
                }
            }
        }

        /// <summary>Handles the 'Validate' button click to check the current data against the schema.</summary>
        private void btnValidateXml_Click(object sender, EventArgs e)
        {
            ValidateCurrentData();
        }

        /// <summary>Handles clicks on the '+' and '-' labels to expand or collapse child UI rows.</summary>
        private void ExpanderLabel_Click(object sender, EventArgs e)
        {
            var label = sender as Label;
            var uiRow = label.Tag as UiRow;
            if (uiRow == null || !uiRow.Children.Any()) return;

            uiRow.IsExpanded = !uiRow.IsExpanded;
            label.Text = uiRow.IsExpanded ? "-" : "+";

            foreach (var childRow in uiRow.Children)
            {
                childRow.RowPanel.Visible = uiRow.IsExpanded;
            }

            if (!uiRow.IsExpanded)
            {
                CollapseAllChildren(uiRow);
            }
        }

        #endregion

        #region UI Generation & Population

        /// <summary>Clears and rebuilds the entire UI layout based on a given XSD schema file.</summary>
        private void PopulateLayoutFromSchema(string schemaPath)
        {
            try
            {
                allUiRows.Clear();
                tlpMainLayout.Visible = false;
                tlpMainLayout.Controls.Clear();
                tlpMainLayout.RowCount = 0;
                tlpMainLayout.RowStyles.Clear();

                XmlSchema schema;
                using (var reader = XmlReader.Create(schemaPath))
                {
                    schema = XmlSchema.Read(reader, null);
                }
                XmlSchemaSet schemaSet = new XmlSchemaSet();
                schemaSet.Add(schema);
                schemaSet.Compile();

                foreach (XmlSchemaElement element in schema.Elements.Values)
                {
                    CreateUiRow(element, 0, null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading or parsing XSD: {ex.Message}", "XSD Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                tlpMainLayout.Visible = true;
            }
        }

        /// <summary>Recursively creates a UI row for a schema element and all its children.</summary>
        private UiRow CreateUiRow(XmlSchemaElement element, int depth, UiRow parent)
        {
            var uiRow = new UiRow { SchemaElement = element, Parent = parent };
            uiRow.RowPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), Height = 30 };

            int indentation = depth * 25;
            var nameLabel = new Label
            {
                Text = element.Name,
                AutoSize = true,
                Location = new Point(indentation + 15, 6)
            };

            List<XmlSchemaElement> childElements = new List<XmlSchemaElement>();
            if (element.ElementSchemaType is XmlSchemaComplexType complexType)
            {
                childElements = GetChildElements(complexType);
            }

            if (childElements.Any())
            {
                uiRow.ExpanderLabel = new Label
                {
                    Text = "+",
                    Font = new Font("Courier New", 9, FontStyle.Bold),
                    Location = new Point(indentation, 4),
                    Size = new Size(15, 15),
                    Tag = uiRow,
                    Cursor = Cursors.Hand
                };
                uiRow.ExpanderLabel.Click += ExpanderLabel_Click;
                uiRow.RowPanel.Controls.Add(uiRow.ExpanderLabel);
            }
            else
            {
                nameLabel.Location = new Point(indentation + 15, 6);
            }

            Control editorControl = CreateEditorControl(element);
            uiRow.RowPanel.Controls.Add(nameLabel);
            if (editorControl != null)
            {
                uiRow.RowPanel.Controls.Add(editorControl);
            }

            var tooltipText = new System.Text.StringBuilder();

            if (element.ElementSchemaType is XmlSchemaComplexType)
            {
                tooltipText.AppendLine($"Complex Type: {element.SchemaTypeName.Name}");
            }
            else if (element.ElementSchemaType is XmlSchemaSimpleType simpleType)
            {
                tooltipText.AppendLine($"Type: {simpleType.TypeCode}");
            }

            if (element.ElementSchemaType is XmlSchemaSimpleType st && st.Content is XmlSchemaSimpleTypeRestriction restriction)
            {
                bool lengthFound = false;
                foreach (XmlSchemaFacet facet in restriction.Facets)
                {
                    if (facet is XmlSchemaLengthFacet len)
                    {
                        tooltipText.AppendLine($"Fixed Length: {len.Value}");
                        lengthFound = true;
                    }
                    else if (facet is XmlSchemaMinLengthFacet minLen)
                    {
                        tooltipText.AppendLine($"Min Length: {minLen.Value}");
                        lengthFound = true;
                    }
                    else if (facet is XmlSchemaMaxLengthFacet maxLen)
                    {
                        tooltipText.AppendLine($"Max Length: {maxLen.Value}");
                        lengthFound = true;
                    }
                }
                if (!lengthFound)
                {
                    tooltipText.AppendLine("Length: Not fixed");
                }
            }

            fieldToolTip.SetToolTip(nameLabel, tooltipText.ToString());
            if (editorControl != null)
            {
                fieldToolTip.SetToolTip(editorControl, tooltipText.ToString());
            }

            tlpMainLayout.RowCount++;
            tlpMainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpMainLayout.Controls.Add(uiRow.RowPanel, 0, tlpMainLayout.RowCount - 1);
            tlpMainLayout.SetColumnSpan(uiRow.RowPanel, 2);

            foreach (var childElement in childElements)
            {
                var childUiRow = CreateUiRow(childElement, depth + 1, uiRow);
                uiRow.Children.Add(childUiRow);
            }

            if (parent != null)
            {
                uiRow.RowPanel.Visible = false;
            }

            allUiRows.Add(uiRow);
            return uiRow;
        }

        /// <summary>Creates an appropriate editor control (TextBox or ComboBox) based on the schema element type.</summary>
        private Control CreateEditorControl(XmlSchemaElement element)
        {
            if (element.ElementSchemaType is XmlSchemaComplexType complexType && complexType.ContentModel is XmlSchemaSimpleContent)
            {
            }
            else if (element.ElementSchemaType is XmlSchemaComplexType)
            {
                return null;
            }

            List<string> enumValues = new List<string>();
            int maxLength = 0;

            if (element.ElementSchemaType is XmlSchemaSimpleType simpleType &&
                simpleType.Content is XmlSchemaSimpleTypeRestriction restriction)
            {
                foreach (XmlSchemaFacet facet in restriction.Facets)
                {
                    if (facet is XmlSchemaEnumerationFacet enumeration)
                    {
                        enumValues.Add(enumeration.Value);
                    }
                    if (facet is XmlSchemaMaxLengthFacet max)
                    {
                        int.TryParse(max.Value, out maxLength);
                    }
                }
            }

            const int pixelsPerChar = 8;
            const int defaultWidth = 300;
            const int minWidth = 75;
            const int maxWidth = 400;

            int controlWidth = defaultWidth;
            if (maxLength > 0)
            {
                int calculatedWidth = maxLength * pixelsPerChar;
                controlWidth = Math.Max(minWidth, Math.Min(calculatedWidth, maxWidth));
            }

            if (enumValues.Any())
            {
                var comboBox = new ComboBox
                {
                    Location = new Point(300, 3),
                    Width = controlWidth,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                comboBox.Items.AddRange(enumValues.ToArray());
                return comboBox;
            }
            else
            {
                var textBox = new TextBox
                {
                    Location = new Point(300, 3),
                    Width = controlWidth,
                };
                if (maxLength > 0)
                {
                    textBox.MaxLength = maxLength;
                }
                return textBox;
            }
        }

        /// <summary>Fills the values of existing editor controls from a loaded XML file.</summary>
        private void PopulateEditorsFromXml(string xmlPath)
        {
            try
            {
                XDocument doc = XDocument.Load(xmlPath);
                if (doc.Root == null) return;

                var xmlElementMap = doc.Descendants().ToLookup(e => e.Name.LocalName);

                foreach (var uiRow in allUiRows)
                {
                    var matchingXmlElements = xmlElementMap[uiRow.SchemaElement.Name].ToList();
                    if (!matchingXmlElements.Any()) continue;

                    var xmlElement = matchingXmlElements.First();

                    var editor = uiRow.RowPanel.Controls.OfType<Control>().LastOrDefault();
                    if (editor is TextBox textBox)
                    {
                        textBox.Text = xmlElement.Nodes().OfType<XText>().FirstOrDefault()?.Value ?? xmlElement.Value;
                    }
                    else if (editor is ComboBox comboBox)
                    {
                        comboBox.SelectedItem = xmlElement.Value;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error reading or parsing XML file: {ex.Message}", "XML Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region XML Building & Saving

        /// <summary>Builds an XDocument from the UI and saves it to the specified file path.</summary>
        private void SaveXmlToFile(string filePath)
        {
            if (!allUiRows.Any())
            {
                MessageBox.Show("There is no data to save.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                UiRow rootUiRow = allUiRows.FirstOrDefault(row => row.Parent == null);
                if (rootUiRow == null)
                {
                    MessageBox.Show("Could not determine the root element to save.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                XElement rootElement = BuildXmlElement(rootUiRow);

                if (rootElement == null)
                {
                    MessageBox.Show("There is no data to save. The XML file was not created.", "Save Cancelled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                XDocument doc = new XDocument(new XDeclaration("1.0", "UTF-8", null), rootElement);
                doc.Save(filePath);

                this.xmlPath = filePath;
                lblStatus.Text = $"File saved successfully to {System.IO.Path.GetFileName(filePath)}";
            }
            catch (Exception ex)
            {
                lblStatus.Text = "Error saving file.";
                MessageBox.Show($"Could not save the file: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        /// <summary>Recursively constructs an XElement from a UI row and its children if they contain data.</summary>
        private XElement BuildXmlElement(UiRow uiRow)
        {
            XName elementName = XName.Get(uiRow.SchemaElement.Name, uiRow.SchemaElement.QualifiedName.Namespace);
            var editor = uiRow.RowPanel.Controls.OfType<Control>().LastOrDefault();

            if (uiRow.Children.Any())
            {
                var childElements = new List<XElement>();
                bool isChoice = false;
                if (uiRow.SchemaElement.ElementSchemaType is XmlSchemaComplexType complexType && complexType.Particle is XmlSchemaChoice)
                {
                    isChoice = true;
                }

                if (isChoice)
                {
                    foreach (var childRow in uiRow.Children)
                    {
                        XElement childElement = BuildXmlElement(childRow);
                        if (childElement != null)
                        {
                            childElements.Add(childElement);
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var childRow in uiRow.Children)
                    {
                        XElement childElement = BuildXmlElement(childRow);
                        if (childElement != null)
                        {
                            childElements.Add(childElement);
                        }
                    }
                }

                if (!childElements.Any())
                {
                    return null;
                }

                var parentElement = new XElement(elementName);
                parentElement.Add(childElements);
                return parentElement;
            }
            else
            {
                string value = null;
                if (editor is TextBox textBox)
                {
                    value = textBox.Text;
                }
                else if (editor is ComboBox comboBox)
                {
                    value = comboBox.SelectedItem?.ToString();
                }

                if (string.IsNullOrWhiteSpace(value))
                {
                    return null;
                }

                var element = new XElement(elementName, value);

                if (uiRow.SchemaElement.SchemaTypeName.Name == "ParaTipi" || uiRow.SchemaElement.SchemaTypeName.Name == "ActiveCurrencyAndAmount" || uiRow.SchemaElement.SchemaTypeName.Name == "ActiveOrHistoricCurrencyAndAmount")
                {
                    element.SetAttributeValue("Ccy", "TRY");
                }

                return element;
            }
        }

        #endregion

        #region XML Validation

        /// <summary>Initiates the validation process for the current data in the UI.</summary>
        private void ValidateCurrentData()
        {
            if (string.IsNullOrEmpty(xsdBodyPath) || !allUiRows.Any())
            {
                MessageBox.Show("Please load an XSD schema and data before validating.", "Cannot Validate", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            validationErrors.Clear();
            ClearErrorHighlights();

            UiRow rootUiRow = allUiRows.FirstOrDefault(row => row.Parent == null);
            if (rootUiRow == null) return;

            XElement rootElement = BuildXmlElement(rootUiRow);
            if (rootElement == null)
            {
                lblStatus.Text = "Validation successful (no data entered).";
                MessageBox.Show("The file is valid as it contains no data.", "Validation Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            XDocument doc = new XDocument(rootElement);
            doc = XDocument.Parse(doc.ToString(), LoadOptions.SetLineInfo);

            XmlSchemaSet schemas = new XmlSchemaSet();
            if (!string.IsNullOrEmpty(xsdHeaderPath))
            {
                schemas.Add(null, xsdHeaderPath);
            }
            schemas.Add(null, xsdBodyPath);

            XmlReaderSettings settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.Schemas = schemas;
            settings.ValidationEventHandler += (sender, e) => ValidationCallBack(sender, e, doc);

            try
            {
                using (XmlReader reader = XmlReader.Create(doc.CreateReader(), settings))
                {
                    while (reader.Read()) { }
                }
            }
            catch (Exception ex)
            {
                validationErrors.Add(ex.Message);
            }

            if (validationErrors.Any())
            {
                lblStatus.Text = $"{validationErrors.Count} validation error(s) found.";
                HighlightErrorControls();
                MessageBox.Show("The data is invalid. Please see highlighted fields.\n\n" + string.Join("\n", validationErrors), "Validation Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            else
            {
                lblStatus.Text = "Validation successful!";
                MessageBox.Show("The current data is valid against the loaded schema.", "Validation Successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /// <summary>A callback method that collects validation errors and identifies the UI control associated with an error.</summary>
        private void ValidationCallBack(object sender, ValidationEventArgs e, XDocument document)
        {
            validationErrors.Add(e.Message);

            int errorLine = e.Exception.LineNumber;
            if (errorLine <= 0) return;

            try
            {
                XElement errorElement = document.Descendants()
                    .FirstOrDefault(x => ((IXmlLineInfo)x).HasLineInfo() && ((IXmlLineInfo)x).LineNumber == errorLine);

                if (errorElement != null)
                {
                    var errorRow = allUiRows.FirstOrDefault(row => row.SchemaElement.Name == errorElement.Name.LocalName);
                    if (errorRow != null)
                    {
                        var editor = errorRow.RowPanel.Controls.OfType<Control>().LastOrDefault();
                        if (editor != null)
                        {
                            errorRow.RowPanel.Tag = editor;
                        }
                    }
                }
            }
            catch
            {
            }
        }

        /// <summary>Changes the background color of UI controls that have been marked with a validation error.</summary>
        private void HighlightErrorControls()
        {
            foreach (var row in allUiRows)
            {
                if (row.RowPanel.Tag is Control controlToHighlight)
                {
                    controlToHighlight.BackColor = Color.LightCoral;
                }
            }
        }

        /// <summary>Resets the background color of all editor controls to the default before a new validation run.</summary>
        private void ClearErrorHighlights()
        {
            foreach (var row in allUiRows)
            {
                row.RowPanel.Tag = null;
                var editor = row.RowPanel.Controls.OfType<Control>().LastOrDefault();
                if (editor != null)
                {
                    editor.BackColor = SystemColors.Window;
                }
            }
        }

        #endregion

        #region UI & Data Helpers

        /// <summary>Finds and returns all direct child elements of a given complex type from the schema.</summary>
        private List<XmlSchemaElement> GetChildElements(XmlSchemaComplexType complexType)
        {
            var childElements = new List<XmlSchemaElement>();

            if (complexType.Particle is XmlSchemaGroupBase group)
            {
                foreach (var item in group.Items)
                {
                    if (item is XmlSchemaElement child)
                    {
                        childElements.Add(child);
                    }
                }
            }
            else if (complexType.ContentModel?.Content is XmlSchemaComplexContentExtension extension)
            {
                if (extension.Particle is XmlSchemaGroupBase extensionGroup)
                {
                    foreach (var item in extensionGroup.Items)
                    {
                        if (item is XmlSchemaElement child)
                        {
                            childElements.Add(child);
                        }
                    }
                }
            }
            return childElements;
        }

        /// <summary>Recursively checks if a UI row or any of its descendants contain user-entered data.</summary>
        private bool HasDataInChildren(UiRow parentRow)
        {
            foreach (var childRow in parentRow.Children)
            {
                var editor = childRow.RowPanel.Controls.OfType<Control>().LastOrDefault();
                if (editor is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text))
                {
                    return true;
                }
                if (editor is ComboBox comboBox && comboBox.SelectedItem != null)
                {
                    return true;
                }

                if (childRow.Children.Any())
                {
                    if (HasDataInChildren(childRow))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>Recursively collapses a UI row and all its descendants, hiding their panels and resetting expander icons.</summary>
        private void CollapseAllChildren(UiRow parentRow)
        {
            foreach (var childRow in parentRow.Children)
            {
                childRow.IsExpanded = false;
                if (childRow.ExpanderLabel != null) childRow.ExpanderLabel.Text = "+";
                childRow.RowPanel.Visible = false;
                if (childRow.Children.Any())
                {
                    CollapseAllChildren(childRow);
                }
            }
        }

        #endregion
    }

    #region Helper Class

    /// <summary>Represents a single dynamic row in the UI, linking schema elements to UI controls.</summary>
    public class UiRow
    {
        public XmlSchemaElement SchemaElement { get; set; }
        public Panel RowPanel { get; set; }
        public Label ExpanderLabel { get; set; }
        public bool IsExpanded { get; set; } = false;
        public UiRow Parent { get; set; }
        public List<UiRow> Children { get; set; } = new List<UiRow>();
    }

    #endregion
}
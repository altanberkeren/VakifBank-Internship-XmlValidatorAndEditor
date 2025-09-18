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
        private string xsdHeaderPath = string.Empty;
        private string xsdBodyPath = string.Empty;
        private string xmlPath = string.Empty;

        private List<string> validationErrors = new List<string>();

        // This is the "master" list that will hold our entire UI structure
        private List<UiRow> allUiRows = new List<UiRow>();

        public MainForm()
        {
            InitializeComponent();
        }

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

                    // This now calls the correct, new method
                    PopulateLayoutFromSchema(xsdBodyPath);
                }
            }
        }

        private void loadXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // CORRECTED: This now checks our new allUiRows list, not the deleted tvSchema
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

        private UiRow CreateUiRow(XmlSchemaElement element, int depth, UiRow parent)
        {
            // A. Create the main object that will hold all info for this row
            var uiRow = new UiRow { SchemaElement = element, Parent = parent }; // <-- THIS LINE IS UPDATED

            // B. Create the Panel that will be the container for the entire row
            uiRow.RowPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), Height = 30 };

            // C. Create the Expander Label (+/-) and the Field Name Label
            int indentation = depth * 25;
            uiRow.ExpanderLabel = new Label
            {
                Text = " ",
                Font = new Font("Courier New", 9, FontStyle.Bold),
                Location = new Point(indentation, 4),
                Size = new Size(15, 15),
                Tag = uiRow
            };
            var nameLabel = new Label
            {
                Text = element.Name,
                AutoSize = true,
                Location = new Point(indentation + 15, 6)
            };

            // D. Create the Editor Control (TextBox or ComboBox)
            Control editorControl = CreateEditorControl(element);

            // E. Add all controls to the row's panel
            uiRow.RowPanel.Controls.Add(uiRow.ExpanderLabel);
            uiRow.RowPanel.Controls.Add(nameLabel);
            if (editorControl != null)
            {
                uiRow.RowPanel.Controls.Add(editorControl);
            }

            // F. Add the new row to the main TableLayoutPanel
            tlpMainLayout.RowCount++;
            tlpMainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpMainLayout.Controls.Add(uiRow.RowPanel, 0, tlpMainLayout.RowCount - 1);
            tlpMainLayout.SetColumnSpan(uiRow.RowPanel, 2);

            // G. Handle the recursive creation of child elements
            bool hasChildren = false;
            if (element.ElementSchemaType is XmlSchemaComplexType complexType)
            {
                if (complexType.Particle is XmlSchemaSequence sequence)
                {
                    foreach (var item in sequence.Items)
                    {
                        if (item is XmlSchemaElement childElement)
                        {
                            hasChildren = true;
                            var childUiRow = CreateUiRow(childElement, depth + 1, uiRow);
                            uiRow.Children.Add(childUiRow);
                        }
                    }
                }
            }

            // H. If the element has children, set the expander symbol and hook up the click event
            if (hasChildren)
            {
                uiRow.ExpanderLabel.Text = "+";
                uiRow.ExpanderLabel.Cursor = Cursors.Hand;
                uiRow.ExpanderLabel.Click += ExpanderLabel_Click;
            }

            // I. Hide all children by default
            if (parent != null)
            {
                uiRow.RowPanel.Visible = false;
            }

            allUiRows.Add(uiRow);
            return uiRow;
        }

        private Control CreateEditorControl(XmlSchemaElement element)
        {
            // A. Check if the element is a complex type (a parent node). If so, we don't create an editor for it.
            if (element.ElementSchemaType is XmlSchemaComplexType)
            {
                return null; // Return null to indicate no editor should be created.
            }

            // B. If it's a simple type, check for enumerations
            List<string> enumValues = new List<string>();
            if (element.ElementSchemaType is XmlSchemaSimpleType simpleType &&
                simpleType.Content is XmlSchemaSimpleTypeRestriction restriction)
            {
                foreach (XmlSchemaFacet facet in restriction.Facets)
                {
                    if (facet is XmlSchemaEnumerationFacet enumeration)
                    {
                        enumValues.Add(enumeration.Value);
                    }
                }
            }

            // C. Create the correct control
            if (enumValues.Any())
            {
                var comboBox = new ComboBox
                {
                    Location = new Point(300, 3),
                    Width = 300,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                // CORRECTED: This properly adds the items to the ComboBox.
                comboBox.Items.AddRange(enumValues.ToArray());
                return comboBox;
            }
            else
            {
                var textBox = new TextBox
                {
                    Location = new Point(300, 3),
                    Width = 300
                };
                return textBox;
            }
        }

        private void ExpanderLabel_Click(object sender, EventArgs e)
        {
            var label = sender as Label;
            var uiRow = label.Tag as UiRow;
            if (uiRow == null) return;

            uiRow.IsExpanded = !uiRow.IsExpanded;

            foreach (var childRow in uiRow.Children)
            {
                childRow.RowPanel.Visible = uiRow.IsExpanded;
            }

            label.Text = uiRow.IsExpanded ? "-" : "+";

            if (!uiRow.IsExpanded)
            {
                CollapseAllChildren(uiRow);
            }
        }

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

        private void saveXMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            {
                // If we have a known path for the XML file, save to it.
                if (!string.IsNullOrEmpty(xmlPath))
                {
                    SaveXmlToFile(xmlPath);
                }
                else
                {
                    // Otherwise, we don't know where to save, so just trigger the "Save As" logic.
                    saveXMLToolStripMenuItem_Click(sender, e);
                }
            }
        }

        // This is the main method that builds and saves the document.
        private void SaveXmlToFile(string filePath)
        {
            if (!allUiRows.Any())
            {
                MessageBox.Show("There is no data to save.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // *** CORRECTED ROOT-FINDING LOGIC ***
                // The root is the element that has no parent. This is 100% reliable.
                UiRow rootUiRow = allUiRows.FirstOrDefault(row => row.Parent == null);
                if (rootUiRow == null)
                {
                    MessageBox.Show("Could not determine the root element to save.", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                XElement rootElement = BuildXmlElement(rootUiRow);
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

        // This recursive helper method traverses our UI structure and builds XElements.
        // This recursive helper method traverses our UI structure and builds XElements.
        private XElement BuildXmlElement(UiRow uiRow)
        {
            // Create a new XML element with the name from the schema
            XName elementName = XName.Get(uiRow.SchemaElement.Name, uiRow.SchemaElement.QualifiedName.Namespace);
            XElement xmlElement = new XElement(elementName);

            // If the row has children (like 'Contact' or 'Address'), recursively build them.
            if (uiRow.Children.Any())
            {
                foreach (var childRow in uiRow.Children)
                {
                    // This is the crucial recursive call that builds the entire tree.
                    xmlElement.Add(BuildXmlElement(childRow));
                }
            }
            // If the row is a simple element (with an editor), get its value.
            else
            {
                var editor = uiRow.RowPanel.Controls.OfType<Control>().LastOrDefault();
                if (editor is TextBox textBox)
                {
                    xmlElement.Value = textBox.Text;
                }
                else if (editor is ComboBox comboBox)
                {
                    xmlElement.Value = comboBox.SelectedItem?.ToString() ?? "";
                }
            }

            return xmlElement;
        }
        private void saveXMLAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog saveFileDialog = new SaveFileDialog())
            {
                saveFileDialog.Filter = "XML Files (*.xml)|*.xml|All files (*.*)|*.*";
                saveFileDialog.Title = "Save an XML File";

                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    // Call our new core saving method with the chosen path
                    SaveXmlToFile(saveFileDialog.FileName);
                }
            }
        }

        // This is the main method that orchestrates the validation.
        // This is the main method that orchestrates the validation.
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
            XDocument doc = new XDocument(rootElement);

            // --- NEW LOGIC TO ADD LINE NUMBER INFO ---
            // We re-parse the XML we just created to add line number information to every element.
            // This is the key to reliably finding the error location later.
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

            // We will now pass our line-info-enabled 'doc' to the callback.
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

        // This method is called automatically by the Validate method for each error found.
        // Note the new signature: we now pass the XDocument in.
        private void ValidationCallBack(object sender, ValidationEventArgs e, XDocument document)
        {
            validationErrors.Add(e.Message);

            // Get the line number from the exception.
            int errorLine = e.Exception.LineNumber;
            if (errorLine <= 0) return;

            try
            {
                // Find the first XML element located at that line number.
                XElement errorElement = document.Descendants()
                    .FirstOrDefault(x => ((IXmlLineInfo)x).HasLineInfo() && ((IXmlLineInfo)x).LineNumber == errorLine);

                if (errorElement != null)
                {
                    // Now that we have the element, we find its corresponding UI row by name.
                    var errorRow = allUiRows.FirstOrDefault(row => row.SchemaElement.Name == errorElement.Name.LocalName);

                    if (errorRow != null)
                    {
                        // Mark the row for highlighting.
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
                // Ignore any errors during the highlighting process.
            }
        }

        // This method is called after validation to color all the "marked" controls.
        private void HighlightErrorControls()
        {
            foreach (var row in allUiRows)
            {
                // We check if we marked this row's panel with a control that has an error.
                if (row.RowPanel.Tag is Control controlToHighlight)
                {
                    controlToHighlight.BackColor = Color.LightCoral;
                }
            }
        }

        // This method resets the color of all controls before a new validation run.
        private void ClearErrorHighlights()
        {
            foreach (var row in allUiRows)
            {
                // Clear the tag and reset the background color for all controls.
                row.RowPanel.Tag = null;
                var editor = row.RowPanel.Controls.OfType<Control>().LastOrDefault();
                if (editor != null)
                {
                    // Use SystemColors.Window to respect the user's current Windows theme.
                    editor.BackColor = SystemColors.Window;
                }
            }
        }


        private void btnValidateXml_Click(object sender, EventArgs e)
        {
            ValidateCurrentData();
        }
    }

    public class UiRow
    {
        public XmlSchemaElement SchemaElement { get; set; }
        public Panel RowPanel { get; set; }
        public Label ExpanderLabel { get; set; }
        public bool IsExpanded { get; set; } = false;
        public UiRow Parent { get; set; }
        public List<UiRow> Children { get; set; } = new List<UiRow>();
    }
}
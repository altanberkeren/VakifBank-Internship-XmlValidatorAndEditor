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
            var uiRow = new UiRow { SchemaElement = element };
            uiRow.RowPanel = new Panel { Dock = DockStyle.Fill, Margin = new Padding(0), Height = 30 };

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

            // This is the only change in this method
            Control editorControl = CreateEditorControl(element);

            uiRow.RowPanel.Controls.Add(uiRow.ExpanderLabel);
            uiRow.RowPanel.Controls.Add(nameLabel);

            // Only add the editor control if one was created
            if (editorControl != null)
            {
                uiRow.RowPanel.Controls.Add(editorControl);
            }

            tlpMainLayout.RowCount++;
            tlpMainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpMainLayout.Controls.Add(uiRow.RowPanel, 0, tlpMainLayout.RowCount - 1);
            tlpMainLayout.SetColumnSpan(uiRow.RowPanel, 2);

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

            if (hasChildren)
            {
                uiRow.ExpanderLabel.Text = "+";
                uiRow.ExpanderLabel.Cursor = Cursors.Hand;
                uiRow.ExpanderLabel.Click += ExpanderLabel_Click;
            }

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
    }

    public class UiRow
    {
        public XmlSchemaElement SchemaElement { get; set; }
        public Panel RowPanel { get; set; }
        public Label ExpanderLabel { get; set; }
        public bool IsExpanded { get; set; } = false;
        public List<UiRow> Children { get; set; } = new List<UiRow>();
    }
}
# XML Validator and Editor

## Overview

The XML Validator and Editor is a user-friendly Windows desktop application built with the .NET Framework. It is designed to dynamically generate an input form from one or more XSD schemas. Users can then use this form to create new XML data, load and edit existing XML files, and validate the data against the schemas to ensure conformity. This tool is particularly useful for working with complex, schema-defined XML structures.

## Features

* **Dynamic UI Generation**: Automatically creates a user interface with appropriate input fields (text boxes, dropdowns) based on an input XSD schema.
* **XML Data Population**: Load existing XML files to easily populate the form fields for viewing or editing.
* **Schema-Based Validation**: Performs validation of the form data against the loaded XSD schema(s).
* **Error Highlighting**: Clearly indicates which fields contain invalid data by highlighting them in red.
* **Detailed Error Reports**: Displays a comprehensive list of all validation errors in a separate window.
* **Schema Constraint Tooltips**: Hovering over a field reveals its schema constraints, such as data type and length restrictions.
* **Save Functionality**: Save the data from the form into a well-formed XML file.
* **Support for Nested Elements**: Handles complex XML structures with nested elements, which can be expanded and collapsed in the UI.

## Screenshots

#### Main Interface
*The application interface after loading an XSD schema and an XML file.*
![Main application interface](files/Ekran%20g%C3%B6r%C3%BCnt%C3%BCs%C3%BC%202025-09-26%20132413.png)
*(Filename: Ekran görüntüsü 2025-09-26 132413.png)*

#### Validation and Error Highlighting
*"Validate" button is clicked, and fields with data that violate the schema rules are highlighted in red.*
![Validation errors highlighted in the UI](files/Ekran%20g%C3%B6r%C3%BCnt%C3%BCs%C3%BC%202025-09-26%20132612.png)
*(Filename: Ekran görüntüsü 2025-09-26 132612.png)*

#### Field Schema Information
*A tooltip showing the data type and length constraints for a selected field.*
![Tooltip with schema information](files/Ekran%20g%C3%B6r%C3%BCnt%C3%BCs%C3%BC%202025-09-26%20132629.png)
*(Filename: Ekran görüntüsü 2025-09-26 132629.png)*

#### Detailed Error Report
*A dialog box providing a complete list of all validation errors found.*
![Detailed validation error report](files/Ekran%20g%C3%B6r%C3%BCnt%C3%BCs%C3%BC%202025-09-26%20132448.jpg)
*(Filename: Ekran görüntüsü 2025-09-26 132448.jpg)*

## How It Works

1.  **Load Schema**: Start by loading the necessary XSD schema files using the **Load Header** and **Load Body** buttons. The application UI is built dynamically based on the structure defined in the "Body" XSD.
2.  **Enter or Load Data**: You can either manually fill in the generated form fields or load an existing XML file using the **Load XML** button to populate the form.
3.  **Validate**: Click the **Validate** button on the toolbar. The application will check the data in every field against the rules defined in the loaded schemas. If any errors are found, the corresponding fields will be highlighted.
4.  **Save**: Once the data is complete and valid, use the **Save** button or the `File > Save` menu options to save the contents of the form as an XML file.

## Technology Stack

* **Language**: C#
* **Framework**: .NET Framework 4.7.2
* **Application Type**: Windows Forms (WinForms)

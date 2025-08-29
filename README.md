# Mountain View Projects Manager

A comprehensive Windows Forms application for managing construction project documentation and contract files with an intuitive interface and advanced navigation capabilities.

## Overview

Mountain View Projects Manager is a desktop application designed to streamline the management of construction project documentation. It provides a structured approach to organizing contracts, documents, and project files across different regions and project types (DMC/CURVE).

## Features

### Core Functionality
- **Region-based Project Organization**: Navigate projects by geographical regions
- **Dual Project Types**: Support for both DMC and CURVE project structures
- **Contract Management**: Organized contract folders with standardized subfolder structure
- **Document Classification**: 22 predefined document types for comprehensive project documentation
- **Intelligent Path Resolution**: Automatic detection and navigation to project folders and files

### User Interface
- **Responsive Design**: Adaptive layout that works across different screen sizes
- **Collapsible Sidebar**: Space-efficient navigation with animated transitions
- **Smart Search**: Real-time project filtering with case-insensitive search
- **Favorites System**: Bookmark frequently accessed regions, projects, and contracts
- **Visual Feedback**: Hover effects, status indicators, and intuitive button states

### File System Integration
- **Cross-platform Compatibility**: Works on Windows, macOS, and Linux
- **Automatic Folder Creation**: Creates missing required subfolders (soft copy, scan, Logs)
- **External Application Launch**: Opens folders and files with system default applications
- **Path Validation**: Real-time checking of folder and file existence

## System Requirements

### Minimum Requirements
- **.NET Framework**: .NET 8.0 or later

### Setup Steps
1. **Download** the latest release from the releases section
2. **Extract** the application files to your desired installation directory
3. **Run** the `ProjectManagerDesigner.exe` file

## Configuration

### Projects.csv Format
CSV file with the following structure:
```csv
Region,Project Name,Project Path
North Region,Project Alpha,C:\Projects\North\Alpha
South Region,Project Beta,C:\Projects\South\Beta
East Region,Project Gamma,C:\Projects\East\Gamma
```

**Column Descriptions**:
- **Region**: Geographical or organizational grouping
- **Project Name**: Display name for the project
- **Project Path**: Full file system path to the project directory

### Custom Branding (Optional)
- **Logo**: Place `logo.png` in the application directory for sidebar branding
- **Icon**: Place `logo.ico` in the application directory for window icon

## Usage Guide

### Getting Started
1. **Launch** the application
2. **Configure** your projects by editing the `Projects.csv` file
3. **Reload Projects** from Settings if you modify the CSV file
4. **Navigate** using the sidebar: Home → Projects → Select Region → Select Project

### Document Management Workflow
1. **Select a Region** from the main projects view
2. **Choose a Project** from the region's project list
3. **Select DMC or CURVE** based on your project type
4. **Pick a Contract** from the available contracts grid
5. **Choose a Document Type** from the 22 predefined categories
6. **Access Files** using the three action buttons (Log, Scan, Soft Copy)

### Document Types
The application supports 22 standardized document categories:
- Shop Drawings, As Built, Transmittal, Letters
- Quantity Survey, RFI, RFA, Material/Document Submittal
- Meeting Minutes (MOM), CVI, Variation Orders/Instructions
- Inspection Requests, MIR, NCR, CPR
- Activity Management, Site Instructions, SWI
- Safety Violations, Daily Reports

### Favorites Management
- **Add to Favorites**: Right-click on any region, project, or contract
- **Access Favorites**: View all favorited items on the Home page
- **Remove Favorites**: Right-click again to toggle favorite status

## File Structure

### Application Files
```
ProjectManagerDesigner/
├── ProjectManagerDesigner.exe     # Main executable
├── Projects.csv                   # Project configuration
├── logo.png                      # Optional custom logo
├── logo.ico                      # Optional window icon
└── [.NET runtime files]
```

### Project Directory Structure
```
Projects/
├── Region 1/
│   ├── Project A/
│   │   ├── DMC/
│   │   │   ├── Contract 1/
│   │   │   │   ├── soft copy/
│   │   │   │   ├── scan/
│   │   │   │   └── Logs/
│   │   │   └── Contract 2/
│   │   └── CURVE/
│   │       └── Contract 3/
│   └── Project B/
└── Region 2/
    └── Project C/
```

## Technical Architecture

### Key Components
- **Form1.cs**: Main application form with UI logic and event handling
- **Form1.Designer.cs**: Visual designer-generated UI component definitions
- **CSV Parser**: Custom implementation for robust CSV file processing
- **Settings System**: JSON-based user preferences and favorites storage
- **Path Resolution**: Intelligent file and folder detection algorithms

### Data Management
- **Project Data**: Loaded from CSV with automatic change detection
- **User Settings**: Persisted as JSON in user application data directory
- **Favorites**: Stored separately in JSON format for portability
- **Caching**: CSV data cached with file modification time tracking

### UI Framework
- **Windows Forms**: Native Windows UI with cross-platform .NET support
- **Responsive Design**: TableLayoutPanel and FlowLayoutPanel for adaptive layouts
- **Custom Controls**: Enhanced buttons with hover effects and state management
- **Animation System**: Smooth sidebar transitions with timer-based animation

---

**Mountain View Projects Manager** - Streamlining Construction Project Documentation

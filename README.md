# GcpvLynx

An application for parsing GCPV CSV files and writing or updating FinishLynx EVT files

## Overview

This application will allow the user to select a race CSV file exported from GCPV. It will parse the file and display the parsed contents, along with the the most likely number of laps based on the "Event :" value. The user can then select a FinishLynx event file (Lynx.evt) to write the parsed content to.

If a race already exists in the EVT file, it will be updated if there are differences (like a racer being added or removed). It will leave identical already existing races unchanged.

The lap count inference may not be perfect, so the user has the ability to specify the number of laps to use when writing out the events from the parsed file to the EVT file.

## GCPV CSV File Format

Race exports from GCPV are CSVs with a variable number of columns. They are generally in the following format:

```
"Event :","1500 111M","Open Men A  male","Stage :","Heat, 2 +2",,,"Race","3A",,"Lane","Skaters","Club",1,"482 Johnson, Michael","Oakville","01-Mar-25   8:41:17 AM"
"Event :","1500 111M","Open Men A  male","Stage :","Heat, 2 +2",,,"Race","3A",,"Lane","Skaters","Club",2,"217 Smith, David","Newmarket","01-Mar-25   8:41:17 AM"
"Event :","1500 111M","Open Men A  male","Stage :","Heat, 2 +2",,,"Race","3A",,"Lane","Skaters","Club",3,"903 Brown, Christopher","Markham","01-Mar-25   8:41:17 AM"
"Event :","1500 111M","Open Men A  male","Stage :","Heat, 2 +2",,,"Race","3A",,"Lane","Skaters","Club",4,"154 Davis, James","London","01-Mar-25   8:41:17 AM"
"Event :","1500 111M","Open Men A  male","Stage :","Heat, 2 +2",,,"Race","3A",,"Lane","Skaters","Club",5,"689 Miller, Daniel","Oakville","01-Mar-25   8:41:17 AM"
"Event :","1500 111M","Open Men A  male","Stage :","Heat, 2 +2",,,"Race","3B",,"Lane","Skaters","Club",1,"598 Taylor, Joshua","St Lawrence","01-Mar-25   8:41:17 AM"
"Event :","1500 111M","Open Men A  male","Stage :","Heat, 2 +2",,,"Race","3B",,"Lane","Skaters","Club",2,"863 Moore, Joseph","Newmarket","01-Mar-25   8:41:17 AM"
"Event :","1500 111M","Open Men A  male","Stage :","Heat, 2 +2",,,"Race","3B",,"Lane","Skaters","Club",3,"121 Jackson, Ryan","Cambridge","01-Mar-25   8:41:17 AM"
"Event :","1500 111M","Open Men A  male","Stage :","Heat, 2 +2",,,"Race","3B",,"Lane","Skaters","Club",4,"955 White, Anthonyx","Oakville","01-Mar-25   8:41:17 AM"
"Event :","1500 111M","Open Men A  male","Stage :","Heat, 2 +2",,,"Race","3B",,"Lane","Skaters","Club",5,"379 Harris, Brian","London","01-Mar-25   8:41:17 AM"
```

Depending on factors like whether it's a heat or a final, there column indexes may be slightly different.

This application parses these rows, looks for the key text "Event: ", "Stage :", "Race", "Lane", "Skater", "Club" and extracts the column with the appropriate offset after the column with these key text values. In this way, what matteres is the position of the values in relation to the key text, not the absolute indexes of the columns.

## Configuration

The application uses an `appsettings.json` file for configuration.

### RaceGroupTrimSuffixes

Sometimes GCPV will add suffixes to the event titles that may read awkwardly if the race names are being displayed anywhere (like on a livestream). This configuration open allows you to specify a list of case sensitive text strings that will be trimmed off the end of the event name when parsing. The strings will be examined in order sorted by longest first.

### DistanceLapMapping

If the value parsed for the Event field contains numbers ("1500 111M"), they will be compared against the mapping configured here to infer the likely number of laps for the race. They will be checked in descending distance lengh order.

### EvtBackupSettings

These settings control whether this application will copy the existing EVT file prior to making any changes to it each time to tell it to update the EVT file. If `BackupsEnabled` is true, the application will create a directory using the name provided in `BackupDirectoryName` inside the directory containing the EVT file if it does not already exist. Each time a change is made, it will copy the currently existing EVT file, and update the name to include the date and time is was backed up. In this way, you can see the entire history of modifications made to the EVT file.

Here is an example configuration:

```
{
  "RaceGroupTrimSuffixes": [
    "male",
    "female",
    "Genders Mixed"
  ],
  "DistanceLapMapping": {
    "1500": 13.5,
    "1000": 9,
    "800": 8,
    "777": 7,
    "500": 4.5,
    "400": 4,
    "333": 3,
    "300": 3,
    "200": 2
  },
  "EvtBackupSettings": {
    "BackupsEnabled": true,
    "BackupDirectoryName": "backups"
   }
}
```



















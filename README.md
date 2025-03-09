The solution contains the following command-line tools.

#### Text Generator
This tool creates text files according to the options provided to the program via specified input parameters.

Available options:
* **-o**: specifies path of an ouput file. If not provided, creates a file in the same directory where the executable file is located.
* **-c**: specifies the total number of lines that an output file may contain.
* **-i**: specifies the maximun number of lines that the line buffer can contain.
* **-n**: specifies the maximum number of words that a line can contain (except the starting number).

#### Text Sort
This tool is designed to sort text lines in the files created by the tool described above.

Available options:
* **-i**: specifies path of an input file.
* **-o**: specifies path of an output file. If not provided, creates a file in the same directory with the input file and names it automatically.
* **-s**: specifies the maximum number of lines per chunk that can be created during text sorting.

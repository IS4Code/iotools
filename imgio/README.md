imgio
==========

This tool operates exclusively on the standard input and output. It attempts to read images from the input one by one, determine their format, save them to a temporary location and the process them by an external command. The command is run with two environment variables (default `%IMG_IN%` and `%IMG_OUT%`; can be set via `-i` and `-o`) which determine the input image location, and the expected output image location. After the image is saved to the output location, the program then reads it and presents it on the standard output. No image delimiters are expected or produced; they are deduced from the structure of the respective image format.

Supported formats are PNG, JPEG, GIF, BMP, RIFF, IFF.

Example: `imgio "copy %IMG_IN% %IMG_OUT%" <image.png >same.png`

"copy %IMG_IN% %IMG_OUT%" is the internal command that gets executed for every image found in the input, in this case consisting of one image, which simpy gets copied to the output location, and saved unchanged to same.png.

Example of using waifu2x to process a video:

`ffmpeg -i in.avi -f image2pipe -c:v png - | imgio "waifu2x-caffe-cui -i %IMG_IN% -o %IMG_OUT%" | ffmpeg -f image2pipe -i - -c copy out.mov`

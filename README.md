# RunApp

Pretty simple utility for URI handling on Windows,
e.g. i'm using it with RubyMine and [Better errors](https://github.com/charliesome/better_errors) gem.

## Installation

Grab archive from [releases page](https://github.com/DeniSix/runapp/releases) and unpack it anywhere.
Then just run `RunApp.exe` and it will ask you for Installation.

To uninstall start `RunApp.exe /uninstall`.

## Configuration

Now you must define your own app in config file `runapp.xml` as follows:

```xml
<?xml version="1.0" encoding="utf-8" ?> 
<RunApp>
	<App key="rubymine" target="%rubymine%\bin\rubymine.exe" args="-l {line} {file}" />
</RunApp>
```

Here you see name for you app (e.g., `rubymine`), target exe, and a command line.
In command line you can use placeholders (e.g., `{line}`, `{file}`) that will be extracted from URI and provided to application arguments.

After that configuration next URI handler will be available:

```
runapp://rubymine?file=c:/test.rb&line=5
```

For example you need to add next line to your's `development.rb` for integrating with Better Errors gem:

```ruby
BetterErrors.editor = 'runapp://rubymine?file=%{file}&line=%{line}' if defined? BetterErrors
```

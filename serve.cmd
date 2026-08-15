@echo off
title uTPro Docs - localhost:4000
cd /d "%~dp0"

echo ========================================
echo   uTPro Docs - Local Preview
echo   http://localhost:4000
echo ========================================
echo.

:: Install gems if needed
if not exist "Gemfile.lock" (
    echo Installing dependencies...
    call bundle install
    echo.
)

echo Starting Jekyll server...
echo Press Ctrl+C to stop.
echo.
call bundle exec jekyll serve --livereload --open-url

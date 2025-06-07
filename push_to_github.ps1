# PowerShell script to push OptionAnalysisTool to GitHub
# Run this after creating the repository on GitHub.com

Write-Host "Pushing OptionAnalysisTool to GitHub..." -ForegroundColor Green

# Add remote origin (replace with your actual repository URL)
git remote add origin https://github.com/AnalystBab/OptionAnalysisTool.git

# Verify remote
git remote -v

# Push to main/master branch
git branch -M main
git push -u origin main

Write-Host "Repository successfully pushed to GitHub!" -ForegroundColor Green
Write-Host "Visit: https://github.com/AnalystBab/OptionAnalysisTool" -ForegroundColor Yellow 
# Graph Report - InternetMeter  (2026-10-29)

## Corpus Check
- 197 files · ~268,716 words
- Verdict: corpus is large enough that graph structure adds value.
- Unclassified: 54 file(s) not represented in the graph (top: .csv 53, (none) 1)

## Summary
- 3952 nodes · 6142 edges · 208 communities (175 shown, 33 thin omitted)
- Extraction: 92% EXTRACTED · 8% INFERRED · 0% AMBIGUOUS · INFERRED: 461 edges (avg confidence: 0.8)
- Token cost: 0 input · 0 output

## Community Hubs (Navigation)
- ShadcnInstaller
- scripts/core.py
- validate_data.py
- gray
- design_system.py
- cip/generate.py
- card
- Window
- slide_search_core.py
- Tailwind CSS Utility Reference
- Python Performance Optimization
- Async Python Patterns Implementation Playbook
- Error Analysis and Resolution Implementation Playbook
- Error Analysis and Resolution Implementation Playbook
- Brand Guidelines v1.0
- Design
- Canvas Design System
- Prerequisites
- spacing
- MainViewModel
- re
- Form & Input Components
- Tailwind CSS Responsive Design
- Code Review Excellence Implementation Playbook
- Typography Specifications
- Logo Usage Rules
- pathlib
- Component Specifications
- html-token-validator.py
- shadcn/ui Accessibility Patterns
- AutoStartService
- Asset Approval Checklist
- Logo AI Prompt Engineering
- Color Palette Management
- CIP Deliverable Guide
- States and Variants
- color
- UI Styling Skill
- argparse
- Workflow
- design-tokens-starter.json
- Capabilities
- Design System
- Tailwind CSS Customization
- ui-visual-validator/SKILL.md
- generate-slide.py
- search-slides.py
- test_design_system_mode.py
- Routing by Task Type
- read_rows
- json
- shadcn/ui Theming & Customization
- TestTailwindConfigGenerator
- AppSettings
- Asset Organization Guide
- Primary Color Meanings
- Core Logo Types
- IReadOnlyList
- Workflow
- 3. Logo Usage
- Brand Consistency Checklist
- CIP Mockup Prompt Engineering
- Color Semantics
- TestNativeDesktopStackFreshness
- ThemeTokens
- .GetRecords
- Design Principles
- Design Principles
- fontSize
- Instructions
- extract-colors.cjs
- CIP Design Reference
- Icon Design Reference
- Copywriting Formulas
- Copywriting Formulas
- Fact
- CatalogRefreshTest
- InternetMeter 🌐⚡
- Banner Design - Multi-Format Creative Banner System
- Messaging Framework
- Brand Voice Framework
- validate-asset.cjs
- Layout Patterns
- Tailwind Integration
- Layout Patterns
- TailwindConfigGenerator
- TestWebStackFreshness
- InternetMeter.Models
- update.md
- Logo Design Reference
- Token Architecture
- .test_node_check_parses_generated_config
- BandwidthAnalyticsViewModel
- TestDomainDetection
- Window
- CultureInfo
- Instructions
- Primitive Tokens
- embed-tokens.cjs
- validate-tokens.cjs
- MainWindow
- TestSearchDomains
- test_data_contracts.py
- Core Visual Elements
- inject-brand-context.cjs
- CIP Design Style Guide
- MainWindow
- DataCapConfigViewModel
- BM25
- sync-brand-to-tokens.cjs
- Brand
- Slide Strategies
- Component Tokens
- generate-tokens.cjs
- duration
- Slide Strategies
- InternetMeter.Tests.csproj
- FakeNetworkService
- _select_palette_for_mode
- Instructions
- Window
- button
- IPingService
- Error Analysis and Resolution
- NativeMethods
- DarkColorTable
- Error Tracking and Monitoring
- Slides Reference
- HTML Slide Template
- HTML Slide Template
- Error Analysis and Resolution
- Error Tracking and Monitoring
- EqualityToBooleanConverter
- Slides
- TestBm25CoreBehavior
- cip/core.py
- input
- Changelog
- IProcessNetworkService
- ProcessNetworkService
- test_text_layout_resilience.py
- TestTokenizer
- Window
- DesignSystemGenerator
- radius
- slides-create.md
- create.md
- RollingHistoryService
- BandwidthAnalyticsWindow
- _palette_is_dark
- ProcessTrafficInfo
- ITrafficJournalService
- test_relevance_evaluator.py
- DataCapServiceTests
- PingService
- DataCapQuotaInfo
- .generate
- ProcessUsageRecord
- Code Review Excellence
- .PlotCanvas_MouseMove
- Async Python Patterns
- TrafficJournalService
- Python Performance Optimization
- RelayCommand
- NetworkAdapterInfo
- BandwidthSample
- PART_Popup
- InlineData
- FakeProcessNetworkService
- DownloadBrush
- NetworkHealthTelemetry
- logo/core.py
- $type
- PingTelemetry
- BM25
- TestStyleTaxonomy
- radius
- lg
- .FormatSpeed
- Converters.cs
- Changed
- Border
- padding-y
- sm
- none
- OpacityToBooleanConverter
- 16
- xl
- 1
- 3
- 8
- destructive
- destructive-foreground
- muted
- primary-foreground
- GraphCanvas
- ring
- secondary-foreground

## God Nodes (most connected - your core abstractions)
1. `MainViewModel` - 176 edges
2. `Window` - 115 edges
3. `AppSettings` - 72 edges
4. `MainWindow` - 66 edges
5. `BandwidthAnalyticsViewModel` - 61 edges
6. `TailwindConfigGenerator` - 58 edges
7. `Window` - 49 edges
8. `InternetMeter.Models` - 44 edges
9. `InternetMeter.Services` - 36 edges
10. `TestTailwindConfigGenerator` - 35 edges

## Surprising Connections (you probably didn't know these)
- `Added` --references--> `AppSettings`  [INFERRED]
  CHANGELOG.md → Models/AppSettings.cs
- `Added` --references--> `RollingHistoryService`  [INFERRED]
  CHANGELOG.md → Services/RollingHistoryService.cs
- `Added` --references--> `TrafficJournalService`  [INFERRED]
  CHANGELOG.md → Services/TrafficJournalService.cs
- `Fixed` --references--> `EqualityToBooleanConverter`  [INFERRED]
  CHANGELOG.md → Converters/Converters.cs
- `Fixed` --references--> `InverseBooleanToVisibilityConverter`  [INFERRED]
  CHANGELOG.md → Converters/Converters.cs

## Import Cycles
- None detected.

## Communities (208 total, 33 thin omitted)

### Community 0 - "ShadcnInstaller"
Cohesion: 0.05
Nodes (3): main(), ShadcnInstaller, TestShadcnInstaller

### Community 1 - "scripts/core.py"
Cohesion: 0.06
Nodes (31): BM25, _contains_phrase(), detect_domain(), _domain_keywords(), _exact_match_diagnostic(), _exact_row_identity(), _exact_stack_identifier(), _file_signature() (+23 more)

### Community 2 - "validate_data.py"
Cohesion: 0.06
Nodes (44): read_rows(), TestAccessibilityGuidance, TestChartsTypographyAndIcons, TestCurrentReactGuidance, TestSemanticColors, TestGeneratedCatalogContract, _catalog_date(), _check_app_interface_contract() (+36 more)

### Community 3 - "gray"
Cohesion: 0.05
Nodes (53): $type, $value, $type, $value, $type, $value, $type, $value (+45 more)

### Community 4 - "design_system.py"
Cohesion: 0.06
Nodes (16): ansi_ljust(), _detect_page_type(), format_ascii_box(), format_markdown(), format_master_md(), format_page_override_md(), generate_design_system(), _generate_intelligent_overrides() (+8 more)

### Community 5 - "cip/generate.py"
Cohesion: 0.19
Nodes (7): build_cip_prompt(), check_logo_required(), generate_cip_set(), generate_with_nano_banana(), load_env(), load_logo_image(), main()

### Community 6 - "card"
Cohesion: 0.20
Nodes (12): $type, $value, bg, bg, padding, shadow, card, bg (+4 more)

### Community 7 - "Window"
Cohesion: 0.03
Nodes (75): AlwaysOnTop, BufferbloatColorHex, BufferbloatStatusText, ConnColorConv, EqualityToBool, InverseBoolToVis, OpacityToBool, CurrentOpacity (+67 more)

### Community 8 - "slide_search_core.py"
Cohesion: 0.13
Nodes (11): calculate_pattern_break(), detect_domain(), get_background_config(), get_color_for_emotion(), get_layout_for_goal(), get_typography_for_slide(), _load_csv(), _load_decision_csv() (+3 more)

### Community 9 - "Tailwind CSS Utility Reference"
Cohesion: 0.05
Nodes (43): Arbitrary Values, Aspect Ratio, Background Colors, Border Color, Border Radius, Border Style, Border Width, Borders (+35 more)

### Community 10 - "Python Performance Optimization"
Cohesion: 0.05
Nodes (41): 1. Profiling Types, 2. Performance Metrics, 3. Optimization Strategies, Advanced Optimization, Basic Timing, Benchmarking Tools, Best Practices, Common Pitfalls (+33 more)

### Community 11 - "Async Python Patterns Implementation Playbook"
Cohesion: 0.05
Nodes (36): 1. Event Loop, 1. Forgetting await, 1. Use Connection Pools, 2. Batch Operations, 2. Blocking the Event Loop, 2. Coroutines, 3. Avoid Blocking Operations, 3. Not Handling Cancellation (+28 more)

### Community 12 - "Error Analysis and Resolution Implementation Playbook"
Cohesion: 0.05
Nodes (37): Alert Configuration, Common Stack Trace Patterns, Communication Templates, Correlation ID Pattern, DataDog APM Integration, Debugging Workflow, Distributed Systems Debugging, Error Aggregation and Pattern Recognition (+29 more)

### Community 13 - "Error Analysis and Resolution Implementation Playbook"
Cohesion: 0.05
Nodes (37): Alert Configuration, Common Stack Trace Patterns, Communication Templates, Correlation ID Pattern, DataDog APM Integration, Debugging Workflow, Distributed Systems Debugging, Error Aggregation and Pattern Recognition (+29 more)

### Community 14 - "Brand Guidelines v1.0"
Cohesion: 0.05
Nodes (37): 1. Color Palette, 2. Typography, 3. Logo Usage, 4. Voice & Tone, 5. Imagery Guidelines, 6. Design Components, Accessibility, AI Image Generation (+29 more)

### Community 15 - "Design"
Cohesion: 0.06
Nodes (35): Banner Design (Built-in), Banner: Design Rules, Banner: Quick Size Reference, Banner: Top Art Styles, Banner: Workflow, CIP Design (Built-in), CIP: Generate Brief, CIP: Generate Mockups (+27 more)

### Community 16 - "Canvas Design System"
Cohesion: 0.06
Nodes (35): 1. Visual Communication First, 2. Minimal Text Integration, 3. Expert Craftsmanship, 4. Systematic Patterns, Analog Meditation, Approach, Canvas Boundaries, Canvas Design System (+27 more)

### Community 17 - "Prerequisites"
Cohesion: 0.06
Nodes (34): Accessibility, Available Domains, Available Stacks, Common Rules for Professional UI, Common Sticking Points, Example Workflow, How to Use This Skill, Icons & Visual Elements (+26 more)

### Community 18 - "spacing"
Cohesion: 0.09
Nodes (22): $type, $value, $type, $value, $type, $value, $type, $value (+14 more)

### Community 19 - "MainViewModel"
Cohesion: 0.02
Nodes (98): MainViewModel, AccentGlowBrush, AccentGlowHex, Adapters, AlwaysOnTop, BufferbloatColorHex, BufferbloatStatusText, CardBackgroundBrush (+90 more)

### Community 20 - "re"
Cohesion: 0.11
Nodes (13): apply_color(), apply_viewbox_size(), extract_svgs(), generate_batch(), generate_icon(), generate_sizes(), load_env(), main() (+5 more)

### Community 21 - "Form & Input Components"
Cohesion: 0.06
Nodes (32): Accordion, Alert, Alert Dialog, Avatar, Badge, Button, Card, Checkbox (+24 more)

### Community 22 - "Tailwind CSS Responsive Design"
Cohesion: 0.06
Nodes (32): 1. Mobile-First Design, 2. Consistent Breakpoint Usage, 3. Test at Breakpoint Boundaries, 4. Use Container for Content Width, 5. Progressive Enhancement, 6. Avoid Too Many Breakpoints, Best Practices, Breakpoint System (+24 more)

### Community 23 - "Code Review Excellence Implementation Playbook"
Cohesion: 0.06
Nodes (30): 1. The Review Mindset, 2. Effective Feedback, 3. Review Scope, Advanced Review Patterns, Best Practices, Code Review Excellence Implementation Playbook, Common Pitfalls, Core Principles (+22 more)

### Community 24 - "Typography Specifications"
Cohesion: 0.06
Nodes (30): Accessibility, Base System, Best Practices, Clean & Modern, Common Font Pairings, Contrast Requirements, CSS Implementation, Editorial (+22 more)

### Community 25 - "Logo Usage Rules"
Cohesion: 0.07
Nodes (28): Absolute Don'ts, Approved Backgrounds, Before Using Logo, Clear Space, Co-branding, Color Rules, Color Usage, Color Variants (+20 more)

### Community 26 - "pathlib"
Cohesion: 0.09
Nodes (6): test_sync_parses_bundled_starter_template(), main(), _run(), test_flags_hardcoded_hex_sharing_line_with_token(), test_token_only_line_reports_no_violation(), read_rows()

### Community 27 - "Component Specifications"
Cohesion: 0.07
Nodes (28): Alert, Anatomy, Anatomy, Anatomy, Anatomy, Anatomy, Badge, Button (+20 more)

### Community 28 - "html-token-validator.py"
Cohesion: 0.13
Nodes (12): get_context(), is_allowed_exception(), is_allowed_rgba(), is_inside_block(), load_css_variables(), main(), print_result(), print_summary() (+4 more)

### Community 29 - "shadcn/ui Accessibility Patterns"
Cohesion: 0.07
Nodes (28): Accordion, Alert, ARIA Labels, Checkbox and Radio, Color Contrast, Command Palette Navigation, Component-Specific Patterns, Dialog/Modal Navigation (+20 more)

### Community 30 - "AutoStartService"
Cohesion: 0.10
Nodes (4): AutoStartService, IAutoStartService, FakeAutoStartService, FakeAutoStartService

### Community 31 - "Asset Approval Checklist"
Cohesion: 0.08
Nodes (25): Accessibility, Archival, Asset Approval Checklist, Automation Support, Color Compliance, Common Issues & Fixes, Content Accessibility, Content Quality (+17 more)

### Community 32 - "Logo AI Prompt Engineering"
Cohesion: 0.08
Nodes (25): Common Pitfalls, Core Prompt Structure, Detailed Brief, Eco/Sustainable, Effective Keywords by Style, Fashion Brand, Healthcare, Industry-Specific Prompts (+17 more)

### Community 33 - "Color Palette Management"
Cohesion: 0.08
Nodes (24): Accessibility Requirements, Brand Compliance Validation, Checking Contrast, Color Documentation Format, Color Extraction, Color Palette Examples, Color Palette Management, Color System Structure (+16 more)

### Community 34 - "CIP Deliverable Guide"
Cohesion: 0.08
Nodes (24): Apparel, Business Card, Car/Sedan, CIP Deliverable Guide, Core Identity, Digital Assets, Email Signature, Envelope (+16 more)

### Community 35 - "States and Variants"
Cohesion: 0.08
Nodes (24): Accessibility, Accessibility Requirements, ARIA States, Color Contrast, Color Variants, Disabled States, Error Messages, Error States (+16 more)

### Community 36 - "color"
Cohesion: 0.11
Nodes (19): $type, $value, background, foreground, muted-foreground, primary, primary-hover, secondary (+11 more)

### Community 37 - "UI Styling Skill"
Cohesion: 0.08
Nodes (24): Accessibility Patterns, Alternative: Tailwind-Only Setup, Best Practices, Common Patterns, Component Layer: shadcn/ui, Component Library Guide, Component + Styling Setup, Core Stack (+16 more)

### Community 38 - "argparse"
Cohesion: 0.12
Nodes (8): generate_html(), get_deliverable_info(), get_image_base64(), main(), format_brief(), format_results(), main(), main()

### Community 39 - "Workflow"
Cohesion: 0.08
Nodes (23): Art Direction Styles (Reuse from Banner), Color & Contrast, Design Best Practices, HTML Design Rules, HTML Template Structure, Option A: Chrome Headless CLI (Recommended — zero dependencies), Option B: chrome-devtools skill, Option C: Playwright script (+15 more)

### Community 40 - "design-tokens-starter.json"
Cohesion: 0.15
Nodes (12): component, $type, $value, dark, semantic, $schema, $type, $value (+4 more)

### Community 41 - "Capabilities"
Cohesion: 0.09
Nodes (22): Accessibility & Inclusive Design, Advanced Design Techniques, Behavioral Traits, Capabilities, Collaboration & Communication, Cross-Platform Design Excellence, Design Research & Validation, Design System Implementation (+14 more)

### Community 42 - "Design System"
Cohesion: 0.09
Nodes (22): Best Practices, Chart.js Integration, Command, Component Spec Pattern, Contextual Decision Flow, Decision System CSVs, Design System, Integration (+14 more)

### Community 43 - "Tailwind CSS Customization"
Cohesion: 0.09
Nodes (22): @apply Directive, Best Practices, Color Customization, Complete Tailwind Config, Configuration Examples, Content Configuration, Custom Color Palette, Custom Font Sizes (+14 more)

### Community 44 - "ui-visual-validator/SKILL.md"
Cohesion: 0.09
Nodes (21): Accessibility Visual Verification, Advanced Validation Techniques, Analysis Process, Automated Visual Testing Integration, Behavioral Traits, Capabilities, Core Principles, Cross-Platform Visual Consistency (+13 more)

### Community 45 - "generate-slide.py"
Cohesion: 0.13
Nodes (11): _e(), generate_chart_slide(), generate_cta_slide(), generate_deck(), generate_metrics_slide(), generate_problem_slide(), generate_solution_slide(), generate_testimonial_slide() (+3 more)

### Community 46 - "search-slides.py"
Cohesion: 0.27
Nodes (5): format_context(), format_result(), main(), search(), search_all()

### Community 47 - "test_design_system_mode.py"
Cohesion: 0.14
Nodes (6): _filter_anti_patterns_for_mode(), _query_wants_dark(), _resolve_color_mode(), _style_is_dark_primary(), TestAntiPatternGating, TestModeResolution

### Community 48 - "Routing by Task Type"
Cohesion: 0.10
Nodes (19): Banner Design Tasks, Brand Identity Tasks, Component Creation, Corporate Identity Program Tasks, Design Routing Guide, Design System Migration, Icon Design Tasks, Implementation Tasks (+11 more)

### Community 49 - "read_rows"
Cohesion: 0.17
Nodes (6): read_rows(), split_values(), style_identities(), TestLandingAndStackContract, TestReasoningContract, TestStyleIdentityContract

### Community 50 - "json"
Cohesion: 0.11
Nodes (11): format_output(), generate_design_brief(), generate_css_for_background(), get_background_image(), get_curated_images(), get_overlay_css(), get_pexels_search_url(), load_backgrounds_config() (+3 more)

### Community 51 - "shadcn/ui Theming & Customization"
Cohesion: 0.10
Nodes (19): Base Color Presets, Best Practices, Color Customization, Color Format, Component Customization, CSS Variable System, Customize Styles, Customize Variants (+11 more)

### Community 53 - "AppSettings"
Cohesion: 0.04
Nodes (44): AppSettings, AlwaysOnTop, BillingCycleResetDay, CompactLeft, CompactTop, CriticalThresholdPercent, DataCapEnabled, DataCapGigabytes (+36 more)

### Community 54 - "Asset Organization Guide"
Cohesion: 0.11
Nodes (18): Asset Entry (manifest.json), Asset Organization Guide, By Campaign, By Status, By Type, Cleanup Workflow, Components, Directory Structure (+10 more)

### Community 55 - "Primary Color Meanings"
Cohesion: 0.11
Nodes (18): Accessibility Considerations, Analogous, Black, Blue, Color Combinations by Industry, Color Harmony Types, Complementary, Green (+10 more)

### Community 56 - "Core Logo Types"
Cohesion: 0.11
Nodes (18): 1. Wordmark (Logotype), 2. Lettermark (Monogram), 3. Pictorial Mark (Brand Mark), 4. Abstract Mark, 5. Mascot, 6. Emblem, 7. Combination Mark, Aesthetic Styles (+10 more)

### Community 57 - "IReadOnlyList"
Cohesion: 0.24
Nodes (5): NetworkAdapterInfo, Description, Id, IsAllSources, Name

### Community 58 - "Workflow"
Cohesion: 0.11
Nodes (17): 10. Prevention, 1. Initial Triage, 2. Observability Data Collection, 3. Hypothesis Generation, 4. Strategy Selection, 5. Intelligent Instrumentation, 6. Production-Safe Techniques, 7. Root Cause Analysis (+9 more)

### Community 59 - "3. Logo Usage"
Cohesion: 0.11
Nodes (17): 3. Logo Usage, 4. Voice & Tone, 5. Imagery Guidelines, Brand Guidelines Template, Brand Personality, Clear Space, Document Structure, Don'ts (+9 more)

### Community 60 - "Brand Consistency Checklist"
Cohesion: 0.11
Nodes (17): Audit Frequency, Brand Consistency Checklist, Channel Audit, Collateral, Colors, Common Issues, Email, Imagery (+9 more)

### Community 61 - "CIP Mockup Prompt Engineering"
Cohesion: 0.11
Nodes (17): Apparel (Polo/T-Shirt), Base Prompt Structure, Business Card, CIP Mockup Prompt Engineering, Context Modifiers, Corporate Minimal, Deliverable-Specific Modifiers, Letterhead (+9 more)

### Community 62 - "Color Semantics"
Cohesion: 0.11
Nodes (17): Accent, Applying Semantic Tokens, Background & Foreground, Border & Ring, Color Semantics, Dark Mode Overrides, Destructive, Interactive States (+9 more)

### Community 64 - "ThemeTokens"
Cohesion: 0.12
Nodes (15): ThemeTokens, AccentGlowBrush, AccentGlowColor, CardBackgroundBrush, CardBackgroundColor, CardBorderBrush, CardBorderColor, DownloadAreaBrush (+7 more)

### Community 65 - ".GetRecords"
Cohesion: 0.16
Nodes (5): TrafficTimeframe, Month, Session, Today, TrafficJournalServiceTests

### Community 66 - "Design Principles"
Cohesion: 0.12
Nodes (15): 22 Art Direction Styles, Banner Sizes & Art Direction Styles Reference, Complete Banner Sizes, CTA Rules, Design Principles, Pinterest Research Queries, Print, Print Specs (+7 more)

### Community 67 - "Design Principles"
Cohesion: 0.12
Nodes (15): 22 Art Direction Styles, Banner Sizes & Art Direction Styles Reference, Complete Banner Sizes, CTA Rules, Design Principles, Pinterest Research Queries, Print, Print Specs (+7 more)

### Community 68 - "fontSize"
Cohesion: 0.12
Nodes (16): $type, $value, $type, $value, $type, $value, $type, $value (+8 more)

### Community 70 - "Instructions"
Cohesion: 0.13
Nodes (14): 1. Error Tracking Analysis, 2. Error Tracking Service Integration, 3. Structured Logging Implementation, 4. Error Alerting Configuration, 5. Error Grouping and Deduplication, 6. Performance Impact Tracking, 7. Error Recovery Strategies, 8. Error Dashboard (+6 more)

### Community 71 - "extract-colors.cjs"
Cohesion: 0.20
Nodes (11): calculateCompliance(), colorDistance(), displayPalette(), extractHexColors(), findNearestBrandColor(), fs, generateImageMagickCommand(), hexToRgb() (+3 more)

### Community 72 - "CIP Design Reference"
Cohesion: 0.13
Nodes (14): CIP Brief (Start Here), CIP Design Reference, Commands, Deliverable Categories, Design Styles, Detailed References, Generate Mockups, HTML Presentation Features (+6 more)

### Community 73 - "Icon Design Reference"
Cohesion: 0.13
Nodes (14): Available Styles, CLI Options, Commands, Generate Batch Variations, Generate Multiple Sizes, Generate Single Icon, Icon Categories, Icon Design Reference (+6 more)

### Community 74 - "Copywriting Formulas"
Cohesion: 0.13
Nodes (14): AIDA (Attention-Interest-Desire-Action), Before-After-Bridge, Contrast Patterns, Copywriting Formulas, Core Formulas, Cost of Inaction, FAB (Features-Advantages-Benefits), Formula-to-Slide Mapping (+6 more)

### Community 75 - "Copywriting Formulas"
Cohesion: 0.13
Nodes (14): AIDA (Attention-Interest-Desire-Action), Before-After-Bridge, Contrast Patterns, Copywriting Formulas, Core Formulas, Cost of Inaction, FAB (Features-Advantages-Benefits), Formula-to-Slide Mapping (+6 more)

### Community 76 - "Fact"
Cohesion: 0.22
Nodes (8): SingleInstanceTests, FakeAutoStartService, FakeNetworkService, FakeProcessNetworkService, Journal, FakeSettingsService, CurrentSettings, ThemeCustomizerTests

### Community 78 - "InternetMeter 🌐⚡"
Cohesion: 0.12
Nodes (16): 1. 🎮 Gaming & VoIP Network Health Monitor, 2. 📊 Monthly Data Cap & Hotspot Quota Alerts, 3. 📈 Rolling 60-Minute Detailed Bandwidth Analytics Window, 4. 🔍 Process Network Inspector & Historical Journal, 5. 🎨 Designer Themes & Windows 11 Acrylic Blur, 6. 👻 Ghost Mode & Screen Edge Snapping, 🛠️ Architecture & Tech Stack, 🚀 Building & Running (+8 more)

### Community 79 - "Banner Design - Multi-Format Creative Banner System"
Cohesion: 0.14
Nodes (13): Art Direction Styles (Top 10), Banner Design - Multi-Format Creative Banner System, Banner Size Quick Reference, Design Rules, Prerequisites, Security, Step 1: Gather Requirements (AskUserQuestion), Step 2: Research & Art Direction (+5 more)

### Community 80 - "Messaging Framework"
Cohesion: 0.14
Nodes (13): Core Statements, Elevator Pitches, Framework Structure, Message Architecture, Message by Audience, Message Testing, Messaging Framework, Mission Statement (+5 more)

### Community 81 - "Brand Voice Framework"
Cohesion: 0.14
Nodes (13): Brand Voice Framework, Character Spectrum, Emotion Spectrum, Language Spectrum, Step 1: Define Personality Traits, Step 2: Create Voice Chart, Step 3: Context Adaptation, Tone Spectrum (+5 more)

### Community 82 - "validate-asset.cjs"
Cohesion: 0.25
Nodes (13): checkManifest(), formatBytes(), formatOutput(), fs, main(), parseFilename(), path, RULES (+5 more)

### Community 83 - "Layout Patterns"
Cohesion: 0.14
Nodes (13): Card Styles, Component Variants, CSS Structures, Feature Grid (3 columns), Layout Decision Flow, Layout Patterns, Layout Selection by Use Case, Metric Styles (+5 more)

### Community 84 - "Tailwind Integration"
Cohesion: 0.14
Nodes (13): Animation Tokens, Base Layer, Button Example, Component Classes, CSS Variables Setup, Dark Mode Toggle, HSL Format Benefits, shadcn/ui Alignment (+5 more)

### Community 85 - "Layout Patterns"
Cohesion: 0.14
Nodes (13): Card Styles, Component Variants, CSS Structures, Feature Grid (3 columns), Layout Decision Flow, Layout Patterns, Layout Selection by Use Case, Metric Styles (+5 more)

### Community 88 - "InternetMeter.Models"
Cohesion: 0.07
Nodes (7): InternetMeter.Views, InternetMeter.ViewModels, InternetMeter.Services, InternetMeter.Common, InternetMeter, InternetMeter.Models, InternetMeter.Tests

### Community 89 - "update.md"
Cohesion: 0.15
Nodes (12): Color Presets, Examples, Files Modified, Important, Overview, Skills Used, Step 1: Gather Brand Input, Step 2: Update Brand Guidelines (+4 more)

### Community 90 - "Logo Design Reference"
Cohesion: 0.15
Nodes (12): Available Styles, Color Psychology, Commands, Design Brief (Start Here), Detailed References, Generate Logo, Industry Defaults, Logo Design Reference (+4 more)

### Community 91 - "Token Architecture"
Cohesion: 0.15
Nodes (12): Categories, Dark Mode, File Organization, Layer 1: Primitive Tokens, Layer 2: Semantic Tokens, Layer 3: Component Tokens, Layer Overview, Migration from Flat Tokens (+4 more)

### Community 93 - "BandwidthAnalyticsViewModel"
Cohesion: 0.05
Nodes (36): BandwidthAnalyticsViewModel, AccentGlowBrush, AverageRateSubtext, AverageRateText, CardBackgroundBrush, CardBorderBrush, CurrentSamples, CurrentTheme (+28 more)

### Community 95 - "Window"
Cohesion: 0.08
Nodes (27): FormattedTotal, FormattedTotalDownload, FormattedTotalUpload, IsActive, PercentageOfTotal, bd, ClearSearchBtn, EmptyStateText (+19 more)

### Community 96 - "CultureInfo"
Cohesion: 0.27
Nodes (3): ConnectionStatusColorConverter, InverseBooleanToVisibilityConverter, MultiplyConverter

### Community 97 - "Instructions"
Cohesion: 0.14
Nodes (13): 1. Analyze Component Requirements, 2. Generate React Component, 3. Generate React Native Component, 4. Generate Component Tests, 5. Generate Styles, 6. Generate Storybook Stories, Context, Do not use this skill when (+5 more)

### Community 98 - "Primitive Tokens"
Cohesion: 0.17
Nodes (11): Border Radius, Color Scales, Gray Scale, Motion / Duration, Primary Colors (Blue), Primitive Tokens, Shadows, Spacing Scale (+3 more)

### Community 99 - "embed-tokens.cjs"
Cohesion: 0.17
Nodes (8): args, fs, minimal, MINIMAL_TOKENS, path, projectRoot, tokensPath, wrapStyle

### Community 100 - "validate-tokens.cjs"
Cohesion: 0.24
Nodes (11): extensions, formatReport(), fs, getFiles(), main(), parseArgs(), path, patterns (+3 more)

### Community 103 - "test_data_contracts.py"
Cohesion: 0.17
Nodes (4): apply_decision_rules(), _object_without_duplicates(), parse_decision_rules(), _validate_action()

### Community 104 - "Core Visual Elements"
Cohesion: 0.18
Nodes (10): Color Palette, Colors, Core Visual Elements, Logo, Logo, Quick Checks, Typography, Typography (+2 more)

### Community 105 - "inject-brand-context.cjs"
Cohesion: 0.31
Nodes (10): extractColorsFromTable(), extractCoreAttributes(), extractHexColors(), extractImageStyle(), extractTypography(), extractVoice(), fs, generatePromptAddition() (+2 more)

### Community 106 - "CIP Design Style Guide"
Cohesion: 0.18
Nodes (10): Bold Dynamic, CIP Design Style Guide, Classic Traditional, Color Psychology, Corporate Minimal, Fresh Modern, Luxury Premium, Modern Tech (+2 more)

### Community 107 - "MainWindow"
Cohesion: 0.08
Nodes (6): [1.5.0] — 2026-10-29, Added, Fixed, MainContextMenu, MainWindow, ContextMenu

### Community 108 - "DataCapConfigViewModel"
Cohesion: 0.07
Nodes (15): IDataCapService, CurrentQuota, DataCapConfigViewModel, BillingCyclePreviewText, BillingCycleResetDay, CancelCommand, CriticalThresholdPercent, DataCapGigabytes (+7 more)

### Community 110 - "sync-brand-to-tokens.cjs"
Cohesion: 0.29
Nodes (8): adjustBrightness(), { execFileSync }, extractColorsFromMarkdown(), fs, generateColorScale(), main(), path, updateDesignTokens()

### Community 111 - "Brand"
Cohesion: 0.20
Nodes (9): Brand, Brand Sync Workflow, Quick Start, References, Routing, Scripts, Subcommands, Templates (+1 more)

### Community 112 - "Slide Strategies"
Cohesion: 0.20
Nodes (9): Common Structures, Duarte Sparkline Pattern, Matching Strategy to Context, Product Demo (6 slides), Sales Pitch (9 slides), Search Commands, Slide Strategies, Strategy Selection (+1 more)

### Community 113 - "Component Tokens"
Cohesion: 0.20
Nodes (9): Alert Tokens, Badge Tokens, Button Tokens, Card Tokens, Component Tokens, Dialog/Modal Tokens, Input Tokens, Table Tokens (+1 more)

### Community 114 - "generate-tokens.cjs"
Cohesion: 0.36
Nodes (9): flattenTokens(), fs, generateCSS(), generateTailwind(), main(), parseArgs(), path, resolveReference() (+1 more)

### Community 115 - "duration"
Cohesion: 0.20
Nodes (10): fast, normal, slow, $type, $value, $type, $value, duration (+2 more)

### Community 116 - "Slide Strategies"
Cohesion: 0.20
Nodes (9): Common Structures, Duarte Sparkline Pattern, Matching Strategy to Context, Product Demo (6 slides), Sales Pitch (9 slides), Search Commands, Slide Strategies, Strategy Selection (+1 more)

### Community 119 - "InternetMeter.Tests.csproj"
Cohesion: 0.20
Nodes (8): net8.0-windows, Microsoft.NET.Sdk, coverlet.collector (6.0.0), Microsoft.NET.Test.Sdk (17.8.0), xunit (2.5.3), xunit.runner.visualstudio (2.5.3), net8.0-windows, Microsoft.NET.Sdk

### Community 120 - "FakeNetworkService"
Cohesion: 0.19
Nodes (5): FakeAutoStartService, FakeNetworkService, FakeSettingsService, CurrentSettings, GhostModeTests

### Community 121 - "_select_palette_for_mode"
Cohesion: 0.22
Nodes (4): _contrast_ratio(), _derive_dark_palette(), _select_palette_for_mode(), TestPaletteSelection

### Community 122 - "Instructions"
Cohesion: 0.17
Nodes (11): 1. Error Tracking Analysis, 2. Error Tracking Service Integration, 3. Structured Logging Implementation, 4. Error Alerting Configuration, 5. Error Grouping and Deduplication, 6. Performance Impact Tracking, 7. Error Recovery Strategies, 8. Error Dashboard (+3 more)

### Community 123 - "Window"
Cohesion: 0.09
Nodes (16): BillingCyclePreviewText, BillingCycleResetDay, CancelCommand, CriticalThresholdPercent, EnableToastNotifications, IsEnabled, ResetAlertsCommand, SaveCommand (+8 more)

### Community 124 - "button"
Cohesion: 0.20
Nodes (10): fg, font-size, hover-bg, button, $type, $value, $type, $value (+2 more)

### Community 125 - "IPingService"
Cohesion: 0.06
Nodes (16): NetworkHealthTelemetry, IPingService, CurrentHealth, IsRunning, FakePingService, CurrentHealth, IsRunning, FakePingService (+8 more)

### Community 126 - "Error Analysis and Resolution"
Cohesion: 0.22
Nodes (8): Context, Do not use this skill when, Error Analysis and Resolution, Instructions, Requirements, Resources, Safety, Use this skill when

### Community 127 - "NativeMethods"
Cohesion: 0.13
Nodes (8): DWM_SYSTEMBACKDROP_TYPE, DWMSBT_AUTO, DWMSBT_MAINWINDOW, DWMSBT_NONE, DWMSBT_TABBEDWINDOW, DWMSBT_TRANSIENTWINDOW, IO_COUNTERS, NativeMethods

### Community 130 - "DarkColorTable"
Cohesion: 0.11
Nodes (17): DarkColorTable, CheckBackground, CheckPressedBackground, CheckSelectedBackground, ImageMarginGradientBegin, ImageMarginGradientEnd, ImageMarginGradientMiddle, MenuBorder (+9 more)

### Community 131 - "Error Tracking and Monitoring"
Cohesion: 0.22
Nodes (8): Context, Do not use this skill when, Error Tracking and Monitoring, Instructions, Requirements, Resources, Safety, Use this skill when

### Community 132 - "Slides Reference"
Cohesion: 0.29
Nodes (6): Key Features, Knowledge Base, Slides Reference, Usage, When to Use, Workflow

### Community 133 - "HTML Slide Template"
Cohesion: 0.29
Nodes (6): Animation Classes, Background Images, Base Structure, Chart.js Integration, CSS Variables Reference, HTML Slide Template

### Community 134 - "HTML Slide Template"
Cohesion: 0.29
Nodes (6): Animation Classes, Background Images, Base Structure, Chart.js Integration, CSS Variables Reference, HTML Slide Template

### Community 136 - "Error Analysis and Resolution"
Cohesion: 0.22
Nodes (8): Context, Do not use this skill when, Error Analysis and Resolution, Instructions, Requirements, Resources, Safety, Use this skill when

### Community 137 - "Error Tracking and Monitoring"
Cohesion: 0.22
Nodes (8): Context, Do not use this skill when, Error Tracking and Monitoring, Instructions, Output Format, Requirements, Resources, Use this skill when

### Community 138 - "EqualityToBooleanConverter"
Cohesion: 0.40
Nodes (4): [1.0.1] — 2026-10-29, Added, Fixed, EqualityToBooleanConverter

### Community 139 - "Slides"
Cohesion: 0.33
Nodes (5): References (Knowledge Base), Routing, Slides, Subcommands, When to Use

### Community 141 - "cip/core.py"
Cohesion: 0.12
Nodes (7): BM25, detect_domain(), get_cip_brief(), _load_csv(), search(), search_all(), _search_csv()

### Community 142 - "input"
Cohesion: 0.29
Nodes (8): padding-x, input, $type, $value, focus-ring, padding-x, $type, $value

### Community 143 - "Changelog"
Cohesion: 0.07
Nodes (27): [1.0.0] — 2026-10-29, [1.1.0] — 2026-10-29, [1.1.1] — 2026-10-29, [1.1.2] — 2026-10-29, [1.2.0] — 2026-10-29, [1.3.0] — 2026-10-29, [1.4.0] — 2026-10-29, [1.5.1] — 2026-10-29 (+19 more)

### Community 144 - "IProcessNetworkService"
Cohesion: 0.12
Nodes (8): IProcessNetworkService, Journal, CompactModeTests, FakeNetworkService, FakeProcessNetworkService, Journal, FakeSettingsService, CurrentSettings

### Community 145 - "ProcessNetworkService"
Cohesion: 0.23
Nodes (5): CachedMetadata, ProcessCandidate, ProcessNetworkService, Journal, ProcessSnapshot

### Community 146 - "test_text_layout_resilience.py"
Cohesion: 0.18
Nodes (3): read_rows(), TestTextLayoutDataContracts, TestTextLayoutRetrieval

### Community 148 - "Window"
Cohesion: 0.08
Nodes (28): ActualHeight, ActualWidth, AverageRateSubtext, AverageRateText, ExportCsvCommand, PeakDownloadSubtext, PeakDownloadText, PeakUploadSubtext (+20 more)

### Community 150 - "radius"
Cohesion: 0.19
Nodes (14): $type, $value, $type, $value, $type, $value, primitive, radius (+6 more)

### Community 153 - "RollingHistoryService"
Cohesion: 0.35
Nodes (4): RollingHistoryService, Capacity, Count, RollingHistoryServiceTests

### Community 154 - "BandwidthAnalyticsWindow"
Cohesion: 0.19
Nodes (5): Tab15mBtn, Tab30mBtn, Tab60mBtn, BandwidthAnalyticsWindow, Button

### Community 155 - "_palette_is_dark"
Cohesion: 0.18
Nodes (4): _palette_is_dark(), _relative_luminance(), TestEndToEndCoherence, TestLuminance

### Community 156 - "ProcessTrafficInfo"
Cohesion: 0.09
Nodes (20): ProcessTrafficInfo, DisplayValueMain, DisplayValueSecondary, DownloadSpeedBytes, ExecutablePath, FormattedDownloadSpeed, FormattedTotalData, FormattedUploadSpeed (+12 more)

### Community 157 - "ITrafficJournalService"
Cohesion: 0.20
Nodes (5): ITrafficJournalService, MockJournalService, DlToReturn, RecordsToReturn, UlToReturn

### Community 158 - "test_relevance_evaluator.py"
Cohesion: 0.12
Nodes (3): TestFixtureValidation, TestMetricMath, TestThresholdGate

### Community 159 - "DataCapServiceTests"
Cohesion: 0.11
Nodes (7): DataCapService, CurrentQuota, ISettingsService, SettingsService, DataCapServiceTests, FakeSettingsService, CurrentSettings

### Community 160 - "PingService"
Cohesion: 0.11
Nodes (7): Application, App, PingService, CurrentHealth, IsRunning, FakeAutoStartService, NetworkHealthTests

### Community 161 - "DataCapQuotaInfo"
Cohesion: 0.08
Nodes (23): DataCapQuotaInfo, AlertLevel, CapBytes, CycleEndUtc, CycleStartUtc, DaysRemainingInCycle, FormattedCap, FormattedRemaining (+15 more)

### Community 163 - "ProcessUsageRecord"
Cohesion: 0.10
Nodes (18): ProcessUsageRecord, CombinedBytes, CurrentDownloadSpeed, CurrentUploadSpeed, ExecutablePath, FirstSeenUtc, FormattedCurrentDownload, FormattedCurrentUpload (+10 more)

### Community 164 - "Code Review Excellence"
Cohesion: 0.29
Nodes (6): Code Review Excellence, Do not use this skill when, Instructions, Output Format, Resources, Use this skill when

### Community 165 - ".PlotCanvas_MouseMove"
Cohesion: 0.24
Nodes (6): bd, OuterCard, PlotBorder, TooltipBorder, CardBorderBrush, Border

### Community 166 - "Async Python Patterns"
Cohesion: 0.33
Nodes (5): Async Python Patterns, Do not use this skill when, Instructions, Resources, Use this skill when

### Community 167 - "TrafficJournalService"
Cohesion: 0.17
Nodes (8): ThemeManager, HistoryStorageModel, Days, Version, ProcessDailyUsage, Dl, Ul, TrafficJournalService

### Community 168 - "Python Performance Optimization"
Cohesion: 0.33
Nodes (5): Do not use this skill when, Instructions, Python Performance Optimization, Resources, Use this skill when

### Community 170 - "NetworkAdapterInfo"
Cohesion: 0.06
Nodes (9): INetworkService, AdapterSnapshot, BytesReceived, BytesSent, TimestampStopwatchTicks, NetworkService, FakeNetworkService, FakeNetworkService (+1 more)

### Community 171 - "BandwidthSample"
Cohesion: 0.24
Nodes (4): BandwidthSample, IRollingHistoryService, Capacity, Count

### Community 172 - "PART_Popup"
Cohesion: 0.67
Nodes (3): IsSubmenuOpen, PART_Popup, Popup

### Community 174 - "InlineData"
Cohesion: 0.24
Nodes (3): AutoStartServiceTests, ConverterTests, UnitFormatterTests

### Community 175 - "FakeProcessNetworkService"
Cohesion: 0.20
Nodes (14): FakeAutoStartService, FakeNetworkService, FakePingService, CurrentHealth, IsRunning, FakeProcessNetworkService, Journal, LastSystemDeltaDl (+6 more)

### Community 176 - "DownloadBrush"
Cohesion: 0.15
Nodes (14): DownloadAreaBrush, UploadAreaBrush, DlDot, DownloadAreaPath, DownloadPath, TooltipDlText, TooltipUlText, UlDot (+6 more)

### Community 177 - "NetworkHealthTelemetry"
Cohesion: 0.29
Nodes (7): NetworkGrade, A, APlus, B, C, D, F

### Community 178 - "logo/core.py"
Cohesion: 0.21
Nodes (5): detect_domain(), _load_csv(), search(), search_all(), _search_csv()

### Community 179 - "$type"
Cohesion: 0.60
Nodes (5): $type, $value, border, border, border

### Community 180 - "PingTelemetry"
Cohesion: 0.21
Nodes (7): LatencyTier, Critical, Moderate, Offline, Optimal, PingTelemetry, PingServiceTests

### Community 183 - "radius"
Cohesion: 0.60
Nodes (5): radius, radius, radius, $type, $value

### Community 184 - "lg"
Cohesion: 0.60
Nodes (5): lg, $type, $value, lg, lg

### Community 189 - "Border"
Cohesion: 0.10
Nodes (20): CardBackgroundBrush, BoolToVis, DataCapHasAlertWarning, DataCapProgressBrush, IsDataCapEnabled, IsProcessInspectorExpanded, ShowPingLatency, Bg (+12 more)

### Community 190 - "padding-y"
Cohesion: 0.67
Nodes (4): padding-y, padding-y, $type, $value

### Community 191 - "sm"
Cohesion: 0.60
Nodes (5): sm, sm, sm, $type, $value

### Community 192 - "none"
Cohesion: 0.67
Nodes (4): $type, $value, none, none

### Community 194 - "OpacityToBooleanConverter"
Cohesion: 0.50
Nodes (3): [1.3.1] — 2026-10-29, Fixed, OpacityToBooleanConverter

### Community 195 - "16"
Cohesion: 0.67
Nodes (3): $type, $value, 16

### Community 196 - "xl"
Cohesion: 0.67
Nodes (4): xl, xl, $type, $value

### Community 197 - "1"
Cohesion: 0.67
Nodes (3): $type, $value, 1

### Community 198 - "3"
Cohesion: 0.67
Nodes (3): $type, $value, 3

### Community 199 - "8"
Cohesion: 0.67
Nodes (3): $type, $value, 8

### Community 200 - "destructive"
Cohesion: 0.67
Nodes (3): destructive, $type, $value

### Community 201 - "destructive-foreground"
Cohesion: 0.67
Nodes (3): destructive-foreground, $type, $value

### Community 202 - "muted"
Cohesion: 0.67
Nodes (3): muted, $type, $value

### Community 203 - "primary-foreground"
Cohesion: 0.67
Nodes (3): primary-foreground, $type, $value

### Community 206 - "ring"
Cohesion: 0.67
Nodes (3): ring, $type, $value

### Community 207 - "secondary-foreground"
Cohesion: 0.67
Nodes (3): secondary-foreground, $type, $value

## Knowledge Gaps
- **467 isolated node(s):** `DWMSBT_AUTO`, `DWMSBT_NONE`, `DWMSBT_MAINWINDOW`, `DWMSBT_TRANSIENTWINDOW`, `DWMSBT_TABBEDWINDOW` (+462 more)
  These have ≤1 connection - possible missing edges or undocumented components. (Counts symbols only; 2207 node(s) total have ≤1 connection when file, concept and rationale nodes are included.)
- **33 thin communities (<3 nodes) omitted from report** — run `graphify query` to explore isolated nodes.

## Suggested Questions
_Questions this graph is uniquely positioned to answer:_

- **Why does `MainViewModel` connect `MainViewModel` to `Window`, `IProcessNetworkService`, `ProcessNetworkService`, `RollingHistoryService`, `BandwidthAnalyticsWindow`, `ProcessTrafficInfo`, `ITrafficJournalService`, `AutoStartService`, `DataCapServiceTests`, `PingService`, `DataCapQuotaInfo`, `RelayCommand`, `NetworkAdapterInfo`, `BandwidthSample`, `FakeProcessNetworkService`, `AppSettings`, `IReadOnlyList`, `.FormatSpeed`, `Changed`, `ThemeTokens`, `Fact`, `.GeneratePoints`, `InternetMeter.Models`, `MainWindow`, `MainWindow`, `DataCapConfigViewModel`, `FakeNetworkService`, `IPingService`?**
  _High betweenness centrality (0.088) - this node is a cross-community bridge._
- **Are the 23 inferred relationships involving `MainViewModel` (e.g. with `.OnStartup()` and `Window`) actually correct?**
  _`MainViewModel` has 23 INFERRED edges - model-reasoned connections that need verification._
- **What connects `DWMSBT_AUTO`, `DWMSBT_NONE`, `DWMSBT_MAINWINDOW` to the rest of the system?**
  _467 weakly-connected nodes found - possible documentation gaps or missing edges._
- **Should `ShadcnInstaller` be split into smaller, more focused modules?**
  _Cohesion score 0.053555750658472345 - nodes in this community are weakly interconnected._
- **Why does `DataCapQuotaInfo` connect `DataCapQuotaInfo` to `MainViewModel`, `ProcessUsageRecord`, `DataCapConfigViewModel`, `DataCapServiceTests`?**
  _High betweenness centrality (0.061) - this node is a cross-community bridge._
- **Are the 5 inferred relationships involving `AppSettings` (e.g. with `Added` and `.SettingsService_DefaultValuesAreSound()`) actually correct?**
  _`AppSettings` has 5 INFERRED edges - model-reasoned connections that need verification._
- **Should `scripts/core.py` be split into smaller, more focused modules?**
  _Cohesion score 0.05563093622795115 - nodes in this community are weakly interconnected._
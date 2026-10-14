import fs from 'node:fs';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const repoRootDir = path.resolve(__dirname, '..');

// Documents to include in order
const docSections = [
  {
    category: 'Getting Started & Overview',
    items: [
      {
        title: 'Project Overview & Packages',
        file: 'Readme.md',
        anchor: 'overview-and-packages',
        description: 'Introduction to Majal, core architecture, Roslyn source generator packages, build instructions, and testing.',
        extractSection: (content) => {
          // Extract Overview, NuGet Packages, Building, Running Tests, Samples, and EF Core Integration (stop before Documentation list)
          const match = content.match(/## Overview[\s\S]*?(?=## Documentation)/);
          return match ? match[0] : content;
        }
      },
      {
        title: 'Getting Started with Majal',
        file: 'docs/getting-started.md',
        anchor: 'getting-started',
        description: 'Installation via .NET CLI / NuGet, basic domain model definition, build output, and next steps.'
      },
      {
        title: 'AI Skill: Majal Coding Assistant',
        file: 'docs/majal.skill.md',
        anchor: 'ai-skill-majal-coding-assistant',
        description: 'Comprehensive guidelines and common patterns for AI agents and developers implementing DDD with Majal.'
      }
    ]
  },
  {
    category: 'Domain-Driven Design Core Generators',
    items: [
      {
        title: 'Entities Guide',
        file: 'docs/entities.md',
        anchor: 'entities-guide',
        description: 'Identity-based equality, IEntity<TId>, Id property generation, IComparable, and comparison helpers.'
      },
      {
        title: 'Aggregates Guide',
        file: 'docs/aggregates.md',
        anchor: 'aggregates-guide',
        description: 'Aggregate roots, domain event lifecycle management, IAggregate<TDomainEvent>, Publish, and Clear.'
      },
      {
        title: 'Value Objects Guide',
        file: 'docs/value-objects.md',
        anchor: 'value-objects-guide',
        description: 'Generic [ValueObject<T>], enum-based value objects, complex multi-property [ValueObject], equality caching, and parsing.'
      },
      {
        title: 'Archivables Guide (Soft-Deletion)',
        file: 'docs/archivables.md',
        anchor: 'archivables-guide',
        description: 'Soft-deletion metadata, IArchivable, IsArchived and ArchivedOn properties, and EF Core interceptor support.'
      },
      {
        title: 'Auditables Guide (Creation & Update Tracking)',
        file: 'docs/auditables.md',
        anchor: 'auditables-guide',
        description: 'Creation and update timestamp tracking, IAuditable<DateTimeOffset>, CreatedOn, and UpdatedOn.'
      },
      {
        title: 'Translatables Guide (Multi-Language Support)',
        file: 'docs/translatables.md',
        anchor: 'translatables-guide',
        description: 'Multi-language content management, [Translatable<TLocale>], [TranslatableOptions], and translation completeness validation.'
      },
      {
        title: 'Ordinals Guide (Custom Sort Order)',
        file: 'docs/ordinals.md',
        anchor: 'ordinals-guide',
        description: 'Sequence and sort order management, IOrdinal, and Ordinal uint property.'
      }
    ]
  },
  {
    category: 'DTO Generation & Mapping',
    items: [
      {
        title: 'DTOs Guide (Compile-Time DTO Generation & Projections)',
        file: 'docs/dtos.md',
        anchor: 'dtos-guide',
        description: 'Zero-overhead compile-time forward mapping (FromEntity/Projection), reverse reconstruction (ToEntity), [DtoMember], [DtoIgnore], [DtoFlatten], polymorphism, and query expressions.'
      }
    ]
  },
  {
    category: 'Framework Integration & Samples',
    items: [
      {
        title: 'Entity Framework Core Integration',
        file: 'docs/ef-core.md',
        anchor: 'ef-core-integration',
        description: 'Majal.EntityFrameworkCore package, MajalDbContext<TLocale> base class, automatic SaveChangesInterceptors, query filters, and value converters.'
      },
      {
        title: 'EShop Sample & Web Front-End',
        file: 'samples/EShop.Web/README.md',
        anchor: 'eshop-sample-web-front-end',
        description: 'Full-stack sample architecture exercising every Majal generator over Wolverine.Http, EF Core SQLite, and Nuxt UI.'
      }
    ]
  }
];

function cleanDocContent(content, item) {
  let text = content;
  if (item.extractSection) {
    text = item.extractSection(text);
  }

  // Remove frontmatter if present
  text = text.replace(/^---[\s\S]*?---\s*/, '');

  // Remove leading top-level header (# Title or ## Overview) matching the section
  text = text.replace(/^#\s+[^\r\n]+[\r\n]*/, '');
  text = text.replace(/^##\s+Overview[\r\n]*/, '');

  // Normalize relative markdown links between docs
  text = text.replace(/\[([^\]]+)\]\((?:docs\/)?([a-z0-9-]+)\.md\)/gi, (match, title, doc) => {
    // Find matching anchor
    for (const section of docSections) {
      for (const it of section.items) {
        if (it.file.endsWith(`${doc}.md`)) {
          return `[${title}](#${it.anchor})`;
        }
      }
    }
    return match;
  });

  return text.trim();
}

// 1. Generate llms-full.txt
function generateLlmsFull() {
  const lines = [];

  lines.push('# Majal — Complete Documentation');
  lines.push('');
  lines.push('> Majal is a high-performance C# Roslyn source generator library for implementing Domain-Driven Design (DDD) patterns and zero-overhead DTO mapping in .NET.');
  lines.push('');
  lines.push('This document contains the complete official documentation for the Majal source generator framework, formatted for consumption by Large Language Models (LLMs).');
  lines.push('');
  lines.push('---');
  lines.push('');
  lines.push('## Table of Contents');
  lines.push('');

  for (const section of docSections) {
    lines.push(`### ${section.category}`);
    for (const item of section.items) {
      lines.push(`- [${item.title}](#${item.anchor})`);
    }
    lines.push('');
  }

  lines.push('---');
  lines.push('');

  for (const section of docSections) {
    for (const item of section.items) {
      const filePath = path.resolve(repoRootDir, item.file);
      if (!fs.existsSync(filePath)) {
        console.warn(`File not found: ${filePath}`);
        continue;
      }

      const rawContent = fs.readFileSync(filePath, 'utf-8');
      const cleaned = cleanDocContent(rawContent, item);

      lines.push(`<!-- anchor: ${item.anchor} -->`);
      lines.push(`## [${section.category}] ${item.title}`);
      lines.push(`_Source: [${item.file}](https://github.com/Ayman-Elfaki/Majal/blob/main/${item.file})_`);
      lines.push('');
      lines.push(cleaned);
      lines.push('');
      lines.push('---');
      lines.push('');
    }
  }

  return lines.join('\n');
}

// 2. Generate llms.txt
function generateLlms() {
  const lines = [];

  lines.push('# Majal');
  lines.push('');
  lines.push('> C# Roslyn source generator library for Domain-Driven Design (DDD) patterns and zero-overhead compile-time DTO mapping in .NET.');
  lines.push('');
  lines.push('Majal simplifies DDD in .NET applications by automatically generating boilerplate code at compile time using Roslyn source generators. It provides clean entity identity, aggregate root domain events, generic and enum-based value objects with hash caching, soft-deletion (archivables), auditing timestamps, localized multi-language content (translatables), sequence ordering (ordinals), zero-overhead compile-time DTO projections inspired by Mapster, and first-class EF Core interceptor and convention integrations.');
  lines.push('');
  lines.push('## Documentation');
  lines.push('');

  for (const section of docSections) {
    lines.push(`### ${section.category}`);
    for (const item of section.items) {
      lines.push(`- [${item.title}](/${item.file}): ${item.description}`);
    }
    lines.push('');
  }

  lines.push('## Optional');
  lines.push('');
  lines.push('- [Full Documentation](/llms-full.txt): Complete concatenated documentation for LLM consumption in a single file');
  lines.push('');

  return lines.join('\n');
}

// Write files
const llmsFullContent = generateLlmsFull();
const llmsContent = generateLlms();

const fullPath = path.resolve(repoRootDir, 'llms-full.txt');
const indexPath = path.resolve(repoRootDir, 'llms.txt');

fs.writeFileSync(fullPath, llmsFullContent, 'utf-8');
fs.writeFileSync(indexPath, llmsContent, 'utf-8');

console.log(`Generated: ${fullPath} (${Buffer.byteLength(llmsFullContent)} bytes)`);
console.log(`Generated: ${indexPath} (${Buffer.byteLength(llmsContent)} bytes)`);

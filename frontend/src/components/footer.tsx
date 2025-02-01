export default function Footer() {
  return (
    <footer className="py-8 bg-gray-100 dark:bg-gray-800">
      <div className="container mx-auto px-4 text-center">
        <p className="text-gray-600 dark:text-gray-300">© {new Date().getFullYear()} CMS Pro. All rights reserved.</p>
      </div>
    </footer>
  )
}


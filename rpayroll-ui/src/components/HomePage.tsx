import HomeLayout from '../layout/HomeLayout'

type HomePageProps = {
  token: string
  username: string
  role: string
  onLogout: () => void
}

export default function HomePage({ token, username, role, onLogout }: HomePageProps) {
  return (
    <HomeLayout
      token={token}
      username={username}
      role={role}
      onLogout={onLogout}
    />
  )
}
